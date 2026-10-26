using Borderland.Dreams.Data;
using Borderland.Dreams.Extensions;
using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Middleware;
using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Borderland.Dreams.Services;

public class FundingRoundService(
    ApplicationDbContext context,
    IUserService userService,
    ICloudNameResolver cloudNameResolver,
    IAppTime appTime,
    IMapperFacade mapper
    ) : IFundingRoundService
{
    private enum FundingRoundLoadMode
    {
        Edit,
        Delete
    }

    public async Task<List<FundingRoundViewModel>> GetAll()
    {
        var fundingRounds = await context.FundingRound
            .Where(fr => fr.CloudID == cloudNameResolver.CloudID)
            .OrderBy(fr => fr.StartDate)
            .ToListAsync();

        return mapper.ToViewModels(fundingRounds);
    }

    public async Task<FundingRoundViewModel> BuildCreateViewModel()
    {
        var userCount = await userService.Count();
        var defaultStartDate = await GetDefaultStartDate();

        return new FundingRoundViewModel
        {
            CloudID = cloudNameResolver.CloudID,
            StartDate = defaultStartDate,
            EndDate = defaultStartDate.Date.AddDays(2).EndOfDay(),
            UsersInCloudCount = userCount
        };
    }

    /// <summary>
    /// Gets the default funding round start date, moving it past any active dreaming deadlines.
    /// </summary>
    private async Task<DateTime> GetDefaultStartDate()
    {
        var cloud = await context.Cloud.SingleOrDefaultAsync(cloud => cloud.ID == cloudNameResolver.CloudID);
        // Start tomorrow unless a dreaming deadline requires a later start time.
        var defaultStartDate = appTime.Now.Date.AddDays(1).StartOfDay();
        var latestDreamingDeadline = new DateTime?[]
        {
            cloud?.DreamCreatePeriodEnd,
            cloud?.DreamEditDeadline
        }.Max();

        if (latestDreamingDeadline.HasValue)
        {
            var nextAllowedStartUtc = GetNextMinuteAfter(latestDreamingDeadline.Value);
            var deadlineStartDate = appTime.ToLocal(nextAllowedStartUtc);
            if (deadlineStartDate > defaultStartDate)
                defaultStartDate = deadlineStartDate;
        }

        return defaultStartDate;
    }

    public async Task<FundingRoundServiceResult> Create(FundingRoundViewModel viewModel)
    {
        if (viewModel.CloudID != cloudNameResolver.CloudID)
        {
            return new FundingRoundServiceResult(
                FundingRoundServiceStatus.Error,
                Errors: new Dictionary<string, List<string>>
                {
                    [""] = ["Invalid cloud context for funding round creation."]
                });
        }

        var cloud = await context.Cloud
            .SingleOrDefaultAsync(cloud => cloud.ID == cloudNameResolver.CloudID);
        if (cloud == null)
            return new FundingRoundServiceResult(FundingRoundServiceStatus.NotFound);

        var existingRounds = await context.FundingRound
            .Where(fr => fr.CloudID == cloudNameResolver.CloudID)
            .ToListAsync();

        var errors = ValidateDates(viewModel, existingRounds, cloud);
        AddErrors(errors, ValidateLastRoundSetting(viewModel, existingRounds));
        if (errors.Count > 0)
            return new FundingRoundServiceResult(FundingRoundServiceStatus.Error, Errors: errors);

        if (viewModel.IsLastRound)
        {
            foreach (var existingRound in existingRounds.Where(round => round.IsLastRound))
                existingRound.IsLastRound = false;
        }

        var fundingRound = mapper.ToModel(viewModel);
        using (var transaction = await context.Database.BeginTransactionAsync())
        {
            context.Add(fundingRound);

            // We do need 2 calls to SaveChanges. The first gives the funding round its ID so we can enqueue it and save again.
            await context.SaveChangesAsync();
            FundingRoundOutboxDispatcher.EnqueueSynchronize(context, fundingRound.ID, appTime.UtcNow);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        return new FundingRoundServiceResult(FundingRoundServiceStatus.Success);
    }

    public async Task<FundingRoundServiceResult> GetForEdit(int id)
    {
        return await LoadFundingRound(id, FundingRoundLoadMode.Edit);
    }

    public async Task<FundingRoundServiceResult> Update(FundingRoundViewModel viewModel)
    {
        if (viewModel.CloudID != cloudNameResolver.CloudID)
            return new FundingRoundServiceResult(FundingRoundServiceStatus.NotFound);

        var roundToUpdate = await context.FundingRound
            .SingleOrDefaultAsync(fr => fr.ID == viewModel.ID && fr.CloudID == cloudNameResolver.CloudID);

        if (roundToUpdate == null)
            return new FundingRoundServiceResult(FundingRoundServiceStatus.NotFound);

        if (HasPassed(roundToUpdate))
        {
            return new FundingRoundServiceResult(
                FundingRoundServiceStatus.Error,
                Errors: new Dictionary<string, List<string>>
                {
                    [""] = ["Cannot edit a funding round that has already passed."]
                });
        }

        var existingRounds = await context.FundingRound
            .Where(fr => fr.CloudID == cloudNameResolver.CloudID && fr.ID != viewModel.ID)
            .ToListAsync();

        if (IsActive(roundToUpdate))
        {
            var errors = ValidateActiveRoundUpdate(viewModel, roundToUpdate, existingRounds);
            AddErrors(errors, ValidateLastRoundSetting(viewModel, existingRounds));
            if (errors.Count > 0)
            {
                viewModel.Title = roundToUpdate.Title;
                viewModel.StartDate = roundToUpdate.StartDate;
                viewModel.AllocationPerUser = roundToUpdate.AllocationPerUser;

                return new FundingRoundServiceResult(FundingRoundServiceStatus.Error, Errors: errors);
            }

            // Only allow changing the end date for an active round
            // since changing the start date or allocation would have immediate effects on users.
            roundToUpdate.EndDate = viewModel.EndDate;
            roundToUpdate.MaxPercentagePerDream = viewModel.MaxPercentagePerDream;
            roundToUpdate.IsLastRound = viewModel.IsLastRound;
        }
        else
        {
            var cloud = await context.Cloud
                .SingleOrDefaultAsync(cloud => cloud.ID == roundToUpdate.CloudID);
            if (cloud == null)
                return new FundingRoundServiceResult(FundingRoundServiceStatus.NotFound);

            var errors = ValidateDates(viewModel, existingRounds, cloud);
            AddErrors(errors, ValidateLastRoundSetting(viewModel, existingRounds));
            if (errors.Count > 0)
                return new FundingRoundServiceResult(FundingRoundServiceStatus.Error, Errors: errors);

            roundToUpdate.Title = viewModel.Title;
            roundToUpdate.StartDate = viewModel.StartDate;
            roundToUpdate.EndDate = viewModel.EndDate;
            roundToUpdate.IsLastRound = viewModel.IsLastRound;
            roundToUpdate.MaxPercentagePerDream = viewModel.MaxPercentagePerDream;
            roundToUpdate.AllocationPerUser = viewModel.AllocationPerUser;
        }

        if (roundToUpdate.IsLastRound)
        {
            foreach (var existingRound in existingRounds.Where(round => round.IsLastRound))
                existingRound.IsLastRound = false;
        }

        FundingRoundOutboxDispatcher.EnqueueSynchronize(context, roundToUpdate.ID, appTime.UtcNow);

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await context.FundingRound.AnyAsync(e => e.ID == viewModel.ID))
                return new FundingRoundServiceResult(FundingRoundServiceStatus.NotFound);
            throw;
        }

        return new FundingRoundServiceResult(FundingRoundServiceStatus.Success);
    }

    public async Task<FundingRoundServiceResult> GetForDelete(int id)
    {
        return await LoadFundingRound(id, FundingRoundLoadMode.Delete);
    }

    public async Task<FundingRoundServiceResult> Delete(int id)
    {
        var fundingRound = await context.FundingRound
            .SingleOrDefaultAsync(fr => fr.ID == id && fr.CloudID == cloudNameResolver.CloudID);

        if (fundingRound == null)
            return new FundingRoundServiceResult(FundingRoundServiceStatus.NotFound);

        if (IsActiveOrPassed(fundingRound))
        {
            return new FundingRoundServiceResult(
                FundingRoundServiceStatus.Error,
                Message: "Cannot delete a funding round that is active or has already passed.");
        }

        FundingRoundOutboxDispatcher.EnqueueCancel(context, fundingRound, appTime.UtcNow);
        context.FundingRound.Remove(fundingRound);
        await context.SaveChangesAsync();

        return new FundingRoundServiceResult(FundingRoundServiceStatus.Success);
    }

    public async Task<FundingRoundViewModel?> GetActiveRound(int cloudId)
    {
        var now = appTime.UtcNow;
        var activeRound = await context.FundingRound
            .SingleOrDefaultAsync(fr => fr.CloudID == cloudId &&
                                     fr.StartDate <= now && fr.EndDate >= now);
        if (activeRound == null) return null;
        return mapper.ToViewModel(activeRound);
    }

    public async Task<bool> HasActiveOrPassedFundingRound(int cloudId)
    {
        var now = appTime.UtcNow;
        return await context.FundingRound
            .AnyAsync(fr => fr.CloudID == cloudId && fr.StartDate <= now);
    }

    private async Task<FundingRoundServiceResult> LoadFundingRound(int id, FundingRoundLoadMode mode)
    {
        var fundingRound = await context.FundingRound
            .SingleOrDefaultAsync(fr => fr.ID == id && fr.CloudID == cloudNameResolver.CloudID);

        if (fundingRound == null)
            return new FundingRoundServiceResult(FundingRoundServiceStatus.NotFound);

        if (mode == FundingRoundLoadMode.Edit && HasPassed(fundingRound))
        {
            return new FundingRoundServiceResult(
                FundingRoundServiceStatus.Error,
                Message: "Cannot edit a funding round that has already passed.");
        }

        if (mode == FundingRoundLoadMode.Delete && IsActiveOrPassed(fundingRound))
        {
            return new FundingRoundServiceResult(
                FundingRoundServiceStatus.Error,
                Message: "Cannot delete a funding round that is active or has already passed.");
        }

        return new FundingRoundServiceResult(
            FundingRoundServiceStatus.Success,
            mapper.ToViewModel(fundingRound));
    }

    private Dictionary<string, List<string>> ValidateActiveRoundUpdate(
        FundingRoundViewModel viewModel,
        FundingRound roundToUpdate,
        List<FundingRound> existingRounds)
    {
        var errors = new Dictionary<string, List<string>>();
        var endDateUtc = appTime.ToUtc(viewModel.EndDate);
        var startDateUtc = roundToUpdate.StartDate;

        if (viewModel.EndDate <= appTime.Now)
            AddError(errors, "EndDate", "End date must be in the future.");
        else if (existingRounds.Any(fr => fr.StartDate <= endDateUtc && fr.EndDate >= startDateUtc))
            AddError(errors, "", "Funding round dates cannot overlap with existing rounds.");

        return errors;
    }

    private Dictionary<string, List<string>> ValidateDates(
        FundingRoundViewModel viewModel,
        List<FundingRound> existingRounds,
        Cloud cloud)
    {
        var errors = new Dictionary<string, List<string>>();
        var now = appTime.Now;
        var startDateUtc = appTime.ToUtc(viewModel.StartDate);
        var endDateUtc = appTime.ToUtc(viewModel.EndDate);

        if (existingRounds.Any(fr => startDateUtc <= fr.EndDate && endDateUtc >= fr.StartDate))
            AddError(errors, "", "Funding round dates cannot overlap with existing rounds.");

        if (viewModel.EndDate <= viewModel.StartDate)
            AddError(errors, "EndDate", "End date must be after start date.");

        if (viewModel.EndDate <= now)
            AddError(errors, "EndDate", "End date must be in the future.");

        if (viewModel.StartDate < now)
            AddError(errors, "StartDate", "Start date cannot be in the past.");

        if ((cloud.DreamCreatePeriodEnd.HasValue && startDateUtc <= cloud.DreamCreatePeriodEnd.Value) ||
            (cloud.DreamEditDeadline.HasValue && startDateUtc <= cloud.DreamEditDeadline.Value))
        {
            AddError(errors, "StartDate",
                "Funding round cannot start until after the dreaming period and edit deadline have ended.");
        }

        return errors;
    }

    private Dictionary<string, List<string>> ValidateLastRoundSetting(
        FundingRoundViewModel viewModel,
        List<FundingRound> otherRounds)
    {
        var errors = new Dictionary<string, List<string>>();
        var endDateUtc = appTime.ToUtc(viewModel.EndDate);
        var lastRound = otherRounds.SingleOrDefault(round => round.IsLastRound);

        if (viewModel.IsLastRound &&
            otherRounds.Any(round => round.EndDate > endDateUtc))
        {
            AddError(errors, nameof(viewModel.IsLastRound),
                "A funding round cannot be marked as last while another round ends after it.");
        }
        else if (!viewModel.IsLastRound &&
                 lastRound != null &&
                 endDateUtc > lastRound.EndDate)
        {
            AddError(errors, nameof(viewModel.EndDate),
                "A funding round cannot extend beyond the last funding round.");
        }

        return errors;
    }

    private static void AddErrors(
        Dictionary<string, List<string>> errors,
        Dictionary<string, List<string>> additionalErrors)
    {
        foreach (var (key, messages) in additionalErrors)
        {
            foreach (var message in messages)
                AddError(errors, key, message);
        }
    }

    /// <summary>
    /// Returns the next minute boundary after the specified date and time,
    /// discarding seconds and fractional seconds.
    /// If the time is already on a minute boundary, advances to the next minute.
    /// </summary>
    private static DateTime GetNextMinuteAfter(DateTime dateTime)
    {
        var ticksToNextMinute = TimeSpan.TicksPerMinute - dateTime.Ticks % TimeSpan.TicksPerMinute;
        return new DateTime(dateTime.Ticks + ticksToNextMinute, dateTime.Kind);
    }

    private static void AddError(Dictionary<string, List<string>> errors, string key, string message)
    {
        if (!errors.TryGetValue(key, out var list))
        {
            list = [];
            errors[key] = list;
        }

        list.Add(message);
    }

    // These 3 methods below exist both on the model and the viewmodel. 
    private bool IsActive(FundingRound round) => round.StartDate <= appTime.UtcNow && round.EndDate >= appTime.UtcNow;
    private bool HasPassed(FundingRound round) => round.EndDate < appTime.UtcNow;
    private bool IsActiveOrPassed(FundingRound round) => IsActive(round) || HasPassed(round);
}