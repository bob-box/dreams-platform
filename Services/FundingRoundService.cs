using Borderland.Dreams.Data;
using Borderland.Dreams.Extensions;
using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Mappers;
using Borderland.Dreams.Middleware;
using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Borderland.Dreams.Services;

public class FundingRoundService(
    ApplicationDbContext context,
    IUserService userService,
    ICloudNameResolver cloudNameResolver,
    FundingRoundSchedulerService schedulerService,
    IAppTime appTime,
    MapperFacade mapper
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

        return new FundingRoundViewModel
        {
            CloudID = cloudNameResolver.CloudID,
            StartDate = appTime.Now.Date.AddDays(1).StartOfDay(),
            EndDate = appTime.Now.Date.AddDays(3).EndOfDay(),
            UsersInCloudCount = userCount
        };
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

        var existingRounds = await context.FundingRound
            .Where(fr => fr.CloudID == cloudNameResolver.CloudID)
            .ToListAsync();

        var errors = ValidateDates(viewModel, existingRounds);
        if (errors.Count > 0)
            return new FundingRoundServiceResult(FundingRoundServiceStatus.Error, Errors: errors);

        var fundingRound = mapper.ToModel(viewModel);
        context.Add(fundingRound);
        await context.SaveChangesAsync();

        schedulerService.ScheduleFundingRoundJobs(fundingRound);
        await context.SaveChangesAsync();

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
        }
        else
        {
            var errors = ValidateDates(viewModel, existingRounds);
            if (errors.Count > 0)
                return new FundingRoundServiceResult(FundingRoundServiceStatus.Error, Errors: errors);

            roundToUpdate.Title = viewModel.Title;
            roundToUpdate.StartDate = viewModel.StartDate;
            roundToUpdate.EndDate = viewModel.EndDate;
            roundToUpdate.MaxPercentagePerDream = viewModel.MaxPercentagePerDream;
            roundToUpdate.AllocationPerUser = viewModel.AllocationPerUser;
        }

        schedulerService.ScheduleFundingRoundJobs(roundToUpdate);

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

        schedulerService.CancelFundingRoundJobs(fundingRound);
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
        return mapper.ToViewModel(activeRound);
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

        if (viewModel.EndDate <= appTime.Now)
            AddError(errors, "EndDate", "End date must be in the future.");
        else if (existingRounds.Any(fr => fr.StartDate <= viewModel.EndDate && fr.EndDate >= roundToUpdate.StartDate))
            AddError(errors, "", "Funding round dates cannot overlap with existing rounds.");

        return errors;
    }

    private Dictionary<string, List<string>> ValidateDates(
        FundingRoundViewModel viewModel,
        List<FundingRound> existingRounds)
    {
        var errors = new Dictionary<string, List<string>>();
        var now = appTime.Now;

        if (existingRounds.Any(fr => viewModel.StartDate <= fr.EndDate && viewModel.EndDate >= fr.StartDate))
            AddError(errors, "", "Funding round dates cannot overlap with existing rounds.");

        if (viewModel.EndDate <= viewModel.StartDate)
            AddError(errors, "EndDate", "End date must be after start date.");

        if (viewModel.EndDate <= now)
            AddError(errors, "EndDate", "End date must be in the future.");

        if (viewModel.StartDate < now)
            AddError(errors, "StartDate", "Start date cannot be in the past.");

        return errors;
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