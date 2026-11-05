using System.Security.Claims;
using Borderland.Dreams.Authorization;
using Borderland.Dreams.Data;
using Borderland.Dreams.Enums;
using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Middleware;
using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Hangfire;

namespace Borderland.Dreams.Services;

public class DreamService(
    ApplicationDbContext context,
    ICloudNameResolver cloudNameService,
    ITagService tagService,
    IAuthorizationService authorizationService,
    IMapperFacade mapper,
    IHttpContextAccessor httpContextAccessor,
    IFundingRoundService fundingRoundService,
    IAppTime appTime,
    IBackgroundJobClient backgroundJobs,
    ICloudService cloudService
) : IDreamService
{
    private ClaimsPrincipal CurrentUser => httpContextAccessor.HttpContext?.User
                                            ?? throw new InvalidOperationException("No active HTTP context.");

    public async Task<DreamServiceResult> GetFull(int id)
    {
        var dreamFromDb = await GetFullQuery(asNoTracking: true).SingleOrDefaultAsync(d => d.ID == id);
        if (dreamFromDb == null)
            return new DreamServiceResult(DreamServiceStatus.NotFound);

        var dream = mapper.ToViewModel(dreamFromDb)!;
        var authorizationFailure = await AuthorizeAsync(dream, Operations.Read);
        if (authorizationFailure != null)
            return authorizationFailure;

        return new DreamServiceResult(DreamServiceStatus.Success, dream);
    }

    public async Task<DreamServiceResult> GetQuick(int id)
    {
        var dreamFromDb = await context.Dream
            .AsNoTracking()
            .SingleOrDefaultAsync(d => d.ID == id && d.CloudID == cloudNameService.CloudID);
        if (dreamFromDb == null)
            return new DreamServiceResult(DreamServiceStatus.NotFound);

        var dream = mapper.ToViewModel(dreamFromDb);
        return new DreamServiceResult(DreamServiceStatus.Success, dream);
    }

    public async Task<DreamFilterViewModel> BuildDreamFilterViewModel(
        string? searchText,
        DreamStatus? statusFilter,
        int? tagFilter,
        DreamSortOrder sortOrder,
        string? currentUserId,
        bool isAdmin)
    {
        var listQuery = GetFullQuery(asNoTracking: true);

        // Admins are allowed to see all dreams
        if (!isAdmin)
        {
            // Do not show dreams that are in Hidden or Draft status
            // unless it is your own dream.
            listQuery = listQuery.Where(x =>
                ((x.Status != DreamStatus.Hidden) && (x.Status != DreamStatus.Draft))
                || x.Dreamers.Any(d => d.UserID == currentUserId));
        }

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            listQuery = listQuery.Where(d =>
                d.Name.Contains(searchText) ||
                d.Description.Contains(searchText));
        }

        if (statusFilter.HasValue)
            listQuery = listQuery.Where(d => d.Status == statusFilter.Value);

        if (tagFilter.HasValue)
            listQuery = listQuery.Where(d => d.Tags.Any(t => t.ID == tagFilter.Value));

        // Sort in db
        listQuery = sortOrder switch
        {
            DreamSortOrder.NameAsc => listQuery.OrderBy(d => d.Name),
            DreamSortOrder.NameDesc => listQuery.OrderByDescending(d => d.Name),
            DreamSortOrder.MostFunded => listQuery.OrderByDescending(d => d.Funds.Sum(f => (double)f.Amount)),
            DreamSortOrder.LeastFunded => listQuery.OrderBy(d => d.Funds.Sum(f => (double)f.Amount)),
            DreamSortOrder.MostFundedPercentage => listQuery.OrderByDescending(d =>
                d.BudgetItems.Sum(b => (double)b.MinAmount) == 0 ? 0 : d.Funds.Sum(f => (double)f.Amount) / d.BudgetItems.Sum(b => (double)b.MinAmount)),
            DreamSortOrder.LeastFundedPercentage => listQuery.OrderBy(d =>
                d.BudgetItems.Sum(b => (double)b.MinAmount) == 0 ? 0 : d.Funds.Sum(f => (double)f.Amount) / d.BudgetItems.Sum(b => (double)b.MinAmount)),
            DreamSortOrder.MostBudget => listQuery.OrderByDescending(d => d.BudgetItems.Sum(b => (double)b.MinAmount)),
            DreamSortOrder.LeastBudget => listQuery.OrderBy(d => d.BudgetItems.Sum(b => (double)b.MinAmount)),
            _ => listQuery.OrderBy(d => d.Name),
        };

        var dreams = await listQuery.ToListAsync();
        var viewModels = dreams.Select(d => mapper.ToViewModel(d)!).ToList();
        var availableTags = await tagService.GetAll();

        return new DreamFilterViewModel
        {
            SearchText = searchText,
            StatusFilter = statusFilter,
            TagFilter = tagFilter,
            SortOrder = sortOrder,
            Dreams = viewModels,
            AvailableTags = availableTags
        };
    }

    public async Task<int> Create(DreamViewModel viewModel)
    {
        var dream = mapper.ToModel(viewModel) ?? throw new InvalidOperationException("Failed to map dream.");
        dream.CloudID = cloudNameService.CloudID;
        dream.Status = DreamStatus.Draft;

        if (viewModel.SelectedTagIds.Count > 0)
        {
            dream.Tags = await context.Tag
                .Where(t => viewModel.SelectedTagIds.Contains(t.ID) && t.CloudID == cloudNameService.CloudID)
                .ToListAsync();
        }

        context.Add(dream);
        await context.SaveChangesAsync();
        backgroundJobs.Enqueue<EmailSendingService>(service => service.SendDreamWelcomeAsync(dream.ID));
        return dream.ID;
    }

    public async Task<DreamServiceResult> Update(DreamViewModel viewModel)
    {
        // We need to fetch the dreams dreamers to check authorization
        var dreamToUpdate = await GetFullQuery().SingleOrDefaultAsync(d => d.ID == viewModel.ID);
        if (dreamToUpdate == null)
            return new DreamServiceResult(DreamServiceStatus.NotFound);

        var authorizationResult = await authorizationService.AuthorizeAsync(CurrentUser, mapper.ToViewModel(dreamToUpdate), Operations.Update);
        if (!authorizationResult.Succeeded)
            return new DreamServiceResult(DreamServiceStatus.NotAllowed);

        // Will this error (that there is no main dreamer) ever happen?
        if (!viewModel.Dreamers.Any(x => x.IsMainDreamer))
            throw new InvalidOperationException("A dream must have at least one main dreamer.");

        dreamToUpdate.Name = viewModel.Name;
        dreamToUpdate.Description = viewModel.Description;
        dreamToUpdate.Dreamers = mapper.ToModels(viewModel.Dreamers);
        dreamToUpdate.BudgetItems = mapper.ToModels(viewModel.BudgetItems);
        dreamToUpdate.Tags = await context.Tag
            .Where(t => viewModel.SelectedTagIds.Contains(t.ID) && t.CloudID == cloudNameService.CloudID)
            .ToListAsync();

        await context.SaveChangesAsync();
        return new DreamServiceResult(DreamServiceStatus.Success);
    }

    public async Task<DreamServiceResult> GetForDelete(int id)
    {
        var dreamFromDb = await GetFullQuery(asNoTracking: true).SingleOrDefaultAsync(m => m.ID == id);
        if (dreamFromDb == null)
            return new DreamServiceResult(DreamServiceStatus.NotFound);

        var dream = mapper.ToViewModel(dreamFromDb)!;
        var authorizationFailure = await AuthorizeAsync(dream, Operations.Delete);

        return authorizationFailure ?? new DreamServiceResult(DreamServiceStatus.Success, dream);
    }

    public async Task<DreamServiceResult> Delete(int id)
    {
        var dream = await context.Dream
            .Include(d => d.Dreamers).ThenInclude(d => d.User)
            .SingleOrDefaultAsync(d => d.ID == id && d.CloudID == cloudNameService.CloudID);

        if (dream == null)
            return new DreamServiceResult(DreamServiceStatus.NotFound);

        var authorizationFailure = await AuthorizeAsync(mapper.ToViewModel(dream)!, Operations.Delete);
        if (authorizationFailure != null)
            return authorizationFailure;

        dream.IsDeleted = true;
        await context.SaveChangesAsync();
        return new DreamServiceResult(DreamServiceStatus.Success);
    }

    /// <summary>
    /// Loads a dream for confirmation after verifying that the current user is an admin or one of its dreamers.
    /// </summary>
    public async Task<DreamServiceResult> GetForCancel(int id)
    {
        var dream = await GetFullQuery(asNoTracking: true)
            .SingleOrDefaultAsync(d => d.ID == id);
        if (dream == null)
            return new DreamServiceResult(DreamServiceStatus.NotFound);

        if (dream.Status == DreamStatus.Canceled)
            return new DreamServiceResult(DreamServiceStatus.NotAllowed);

        var dreamViewModel = mapper.ToViewModel(dream)!;
        var authorizationFailure = await AuthorizeAsync(dreamViewModel, Operations.Cancel);
        return authorizationFailure ?? new DreamServiceResult(
            DreamServiceStatus.Success,
            dreamViewModel);
    }

    public async Task<DreamServiceResult> Cancel(int id)
    {
        var dream = await GetFullQuery()
            .SingleOrDefaultAsync(d => d.ID == id);
        if (dream == null)
            return new DreamServiceResult(DreamServiceStatus.NotFound);

        if (dream.Status == DreamStatus.Canceled)
            return new DreamServiceResult(DreamServiceStatus.NotAllowed);

        var authorizationFailure = await AuthorizeAsync(mapper.ToViewModel(dream)!, Operations.Cancel);
        if (authorizationFailure != null)
            return authorizationFailure;

        dream.Status = DreamStatus.Canceled;
        await context.SaveChangesAsync();
        return new DreamServiceResult(DreamServiceStatus.Success);
    }

    public async Task<IReadOnlyList<DreamViewModel>> GetDeletedDreams()
    {
        var dreams = await GetFullQuery(asNoTracking: true, includeDeleted: true)
            .Where(d => d.IsDeleted)
            .OrderByDescending(d => d.CreatedAtUtc)
            .ToListAsync();

        // Get when and who deleted it
        var deletedDreamIds = dreams.Select(d => d.ID).ToList();
        var deletionEntries = await context.AuditLog
            .Where(entry => entry.EntityType == "Dream" &&
                            entry.Action == "Deleted" &&
                            entry.CloudID == cloudNameService.CloudID &&
                            deletedDreamIds.Contains(entry.EntityID))
            .OrderByDescending(entry => entry.CreatedAtUtc)
            .ToListAsync();
        // A dream can be restored and deleted again, so report its most recent deletion event.
        var latestDeletionByDream = deletionEntries
            .GroupBy(entry => entry.EntityID)
            .ToDictionary(group => group.Key, group => group.First());

        return dreams.Select(d =>
        {
            var viewModel = mapper.ToViewModel(d)!;
            if (latestDeletionByDream.TryGetValue(d.ID, out var deletion))
            {
                viewModel.DeletedAt = deletion.CreatedAtUtc.ToLocalTime();
                viewModel.DeletedBy = deletion.UserName;
            }
            return viewModel;
        }).ToList();
    }

    public async Task<DreamServiceResult> GetDeletedDream(int id)
    {
        var dream = await GetFullQuery(asNoTracking: true, includeDeleted: true)
            .SingleOrDefaultAsync(d => d.ID == id && d.IsDeleted);

        return dream == null
            ? new DreamServiceResult(DreamServiceStatus.NotFound)
            : new DreamServiceResult(DreamServiceStatus.Success, mapper.ToViewModel(dream));
    }

    public async Task<DreamServiceResult> RestoreDeletedDream(int id)
    {
        if (!CurrentUser.IsInRole(Constants.AdminRole))
            return new DreamServiceResult(DreamServiceStatus.NotAllowed);

        var dream = await context.Dream
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(d =>
                d.ID == id &&
                d.CloudID == cloudNameService.CloudID &&
                d.IsDeleted);

        if (dream == null)
            return new DreamServiceResult(DreamServiceStatus.NotFound);

        dream.IsDeleted = false;
        await context.SaveChangesAsync();
        return new DreamServiceResult(DreamServiceStatus.Success);
    }

    /// <summary>
    /// Moves a draft dream to ReadyForFunding for dreamers or admins, or to Active for admins once funding has started.
    /// </summary>
    /// <param name="id">Dream id to be published.</param>
    public async Task<DreamServiceResult> Publish(int id)
    {
        var dream = await GetFullQuery()
            .SingleOrDefaultAsync(d => d.ID == id && d.CloudID == cloudNameService.CloudID);

        if (dream == null)
            return new DreamServiceResult(DreamServiceStatus.NotFound);

        var dreamViewModel = mapper.ToViewModel(dream)!;
        var authorizationResult = await authorizationService.AuthorizeAsync(
            CurrentUser,
            dreamViewModel,
            Operations.Publish);
        if (!authorizationResult.Succeeded || dream.Status != DreamStatus.Draft)
            return new DreamServiceResult(DreamServiceStatus.NotAllowed);

        var hasStartedFundingRound = await fundingRoundService.HasActiveOrPassedFundingRound(cloudNameService.CloudID);

        if (CurrentUser.IsInRole(Constants.AdminRole))
        {
            // Admins may make draft dreams "ready" at any time; once funding has begun, it activates them immediately.
            dream.Status = hasStartedFundingRound
                ? DreamStatus.Active
                : DreamStatus.ReadyForFunding;
        }
        else
        {
            // Dreamers can only "ready" drafts during the dreaming phase, before any funding round has started. 
            // After that Admins need to activate them.
            if (hasStartedFundingRound)
                return new DreamServiceResult(DreamServiceStatus.NotAllowed);

            var cloud = await cloudService.GetById(cloudNameService.CloudID);
            if (cloud == null)
                return new DreamServiceResult(DreamServiceStatus.NotFound);

            if (!cloud.IsBeforeEditDeadline(appTime))
                return new DreamServiceResult(DreamServiceStatus.NotAllowed);

            dream.Status = DreamStatus.ReadyForFunding;
        }

        await context.SaveChangesAsync();
        return new DreamServiceResult(DreamServiceStatus.Success);
    }

    public async Task<DreamServiceResult> ReturnToDraft(int id)
    {
        var dream = await GetFullQuery()
            .SingleOrDefaultAsync(d => d.ID == id && d.CloudID == cloudNameService.CloudID);

        if (dream == null)
            return new DreamServiceResult(DreamServiceStatus.NotFound);

        if (dream.Status != DreamStatus.ReadyForFunding)
            return new DreamServiceResult(DreamServiceStatus.NotAllowed);

        var dreamViewModel = mapper.ToViewModel(dream)!;
        var authorizationResult = await authorizationService.AuthorizeAsync(
            CurrentUser,
            dreamViewModel,
            Operations.ReturnToDraft);
        if (!authorizationResult.Succeeded)
            return new DreamServiceResult(DreamServiceStatus.NotAllowed);

        var cloud = await cloudService.GetById(cloudNameService.CloudID);
        if (cloud == null)
            return new DreamServiceResult(DreamServiceStatus.NotFound);
        
        if (!cloud.IsBeforeEditDeadline(appTime))
            return new DreamServiceResult(DreamServiceStatus.NotAllowed);

        dream.Status = DreamStatus.Draft;
        await context.SaveChangesAsync();
        return new DreamServiceResult(DreamServiceStatus.Success);
    }

    public async Task<DreamServiceResult> SetDreamStatusTo(int id, DreamStatus dreamStatus)
    {
        if (!CurrentUser.IsInRole(Constants.AdminRole))
            return new DreamServiceResult(DreamServiceStatus.NotAllowed);

        var dream = await GetFullQuery()
            .SingleOrDefaultAsync(d => d.ID == id && d.CloudID == cloudNameService.CloudID);

        if (dream == null)
            return new DreamServiceResult(DreamServiceStatus.NotFound);

        dream.Status = dreamStatus;
        await context.SaveChangesAsync();
        return new DreamServiceResult(DreamServiceStatus.Success);
    }

    public bool DreamExists(int id, int cloudID)
    {
        return context.Dream.Any(e => e.ID == id && e.CloudID == cloudID);
    }

    public async Task<bool> CanUserEditDreamers(DreamViewModel dream, ClaimsPrincipal user)
    {
        var authorizationResult = await authorizationService.AuthorizeAsync(user, dream, Operations.EditDreamers);
        return authorizationResult.Succeeded;
    }

    /// <summary>
    /// Includes related entities (so you can run AuthorizationService checks).
    /// </summary>
    private IQueryable<Dream> GetFullQuery(bool asNoTracking = false, bool includeDeleted = false)
    {
        int cloudId = cloudNameService.CloudID;

        var dreams = includeDeleted ? context.Dream.IgnoreQueryFilters() : context.Dream;
        var query = dreams
            .Include(d => d.BudgetItems)
            .Include(d => d.Cloud)
            .Include(d => d.Dreamers).ThenInclude(d => d.User)
            .Include(d => d.Funds).ThenInclude(d => d.User)
            .Include(d => d.Tags)
            .Where(d => d.CloudID == cloudId);

        return asNoTracking ? query.AsNoTracking() : query;
    }

    private async Task<DreamServiceResult?> AuthorizeAsync(DreamViewModel dream, OperationAuthorizationRequirement requirement)
    {
        var authorizationResult = await authorizationService.AuthorizeAsync(CurrentUser, dream, requirement);
        return authorizationResult.Succeeded ? null : new DreamServiceResult(DreamServiceStatus.NotAllowed);
    }
}