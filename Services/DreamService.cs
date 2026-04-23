using System.Security.Claims;
using Borderland.Dreams.Authorization;
using Borderland.Dreams.Data;
using Borderland.Dreams.Enums;
using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Mappers;
using Borderland.Dreams.Middleware;
using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Borderland.Dreams.Services;

public class DreamService(
    ApplicationDbContext context,
    ICloudNameResolver cloudNameService,
    ITagService tagService,
    UserManager<User> userManager,
    IAuthorizationService authorizationService,
    IFundingRoundService fundingRoundService,
    MapperFacade mapper,
    ICloudService cloudService,
    IMembershipCheckService membershipCheckService
) : IDreamService
{
    /// <summary>
    /// Includes related entities (so you can run AuthorizationService checks).
    /// </summary>
    /// <param name="cloudId">Cloud ID that the dream belongs to</param>
    /// <returns></returns>
    public IQueryable<Dream> GetFullQuery()
    {
        int cloudId = cloudNameService.CloudID;

        return context.Dream
            .Include(d => d.BudgetItems)
            .Include(d => d.Dreamers).ThenInclude(d => d.User)
            .Include(d => d.Funds).ThenInclude(d => d.User)
            .Include(d => d.Tags)
            .Where(d => d.CloudID == cloudId);
    }

    public async Task<DreamViewModel?> GetQuick(int id)
    {
        var dream = await context.Dream.SingleOrDefaultAsync(d => d.ID == id && d.CloudID == cloudNameService.CloudID);
        return dream != null ? mapper.ToViewModel(dream) : null;
    }

    public async Task<DreamFilterViewModel> BuildDreamFilterViewModel(
        string? searchText,
        DreamStatus? statusFilter,
        int? tagFilter,
        DreamSortOrder sortOrder,
        string? currentUserId,
        bool isAdmin)
    {
        var listQuery = GetFullQuery();

        if (!isAdmin)
        {
            listQuery = listQuery.Where(x =>
                (x.Status != DreamStatus.Hidden && x.Status != DreamStatus.Draft)
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

        var viewModels = listQuery.Select(mapper.ToViewModel).ToList();
        viewModels = sortOrder switch
        {
            DreamSortOrder.NameDesc => [.. viewModels.OrderByDescending(d => d.Name)],
            DreamSortOrder.MostFunded => [.. viewModels.OrderByDescending(d => d.TotalReceivedFunds)],
            DreamSortOrder.LeastFunded => [.. viewModels.OrderBy(d => d.TotalReceivedFunds)],
            DreamSortOrder.MostFundedPercentage => [.. viewModels.OrderByDescending(d =>
                d.TotalRequestedBudget == 0 ? 0 : d.TotalReceivedFunds / d.TotalRequestedBudget)],
            DreamSortOrder.LeastFundedPercentage => [.. viewModels.OrderBy(d =>
                d.TotalRequestedBudget == 0 ? 0 : d.TotalReceivedFunds / d.TotalRequestedBudget)],
            DreamSortOrder.MostBudget => [.. viewModels.OrderByDescending(d => d.TotalRequestedBudget)],
            DreamSortOrder.LeastBudget => [.. viewModels.OrderBy(d => d.TotalRequestedBudget)],
            _ => [.. viewModels.OrderBy(d => d.Name)],
        };

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
        var dream = mapper.ToModel(viewModel);
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
        return dream.ID;
    }

    public async Task Update(Dream dreamToUpdate, DreamViewModel viewModel)
    {
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
    }

    public async Task<DreamDeleteResult> GetForDelete(int id)
    {
        var dreamFromDb = await GetFullQuery().SingleOrDefaultAsync(m => m.ID == id);
        var dream = mapper.ToViewModel(dreamFromDb);
        if (dream == null)
            return new DreamDeleteResult(DreamDeleteStatus.NotFound);

        return new DreamDeleteResult(DreamDeleteStatus.Success, dream);
    }

    public async Task<DreamDeleteResult> Delete(int id)
    {
        var dream = await context.Dream.SingleOrDefaultAsync(d => d.ID == id && d.CloudID == cloudNameService.CloudID);

        if (dream == null)
            return new DreamDeleteResult(DreamDeleteStatus.NotFound);

        context.Dream.Remove(dream);
        await context.SaveChangesAsync();
        return new DreamDeleteResult(DreamDeleteStatus.Success);
    }

    public async Task<FundDreamServiceResult> GetFundingModal(int dreamId, string userId)
    {
        var (ctx, error) = await GetFundingContext(dreamId, userId);
        if (error != null) return error;

        var userFundsForThisDream = await context.Fund
            .Where(f => f.DreamID == dreamId && f.UserID == userId)
            .ToListAsync();

        decimal userFundedInCurrentRound = userFundsForThisDream
            .SingleOrDefault(f => f.FundingRoundID == ctx.ActiveFundingRound.ID)?.Amount ?? 0m;
        decimal userTotalFundedAllRounds = userFundsForThisDream.Sum(f => f.Amount);

        var viewModel = new FundingModalViewModel
        {
            DreamId = dreamId,
            RoundMaxAmount = ctx.ActiveFundingRound.MaxPerDream,
            UserFundedInCurrentRound = userFundedInCurrentRound,
            UserTotalFundedAllRounds = userTotalFundedAllRounds,
            UserWalletBalance = ctx.Wallet.Balance
        };

        return new FundDreamServiceResult(FundDreamServiceStatus.Success, ViewModel: viewModel);
    }

    public async Task<FundDreamServiceResult> FundDream(int dreamId, decimal amountToFund, string userId)
    {
        if (amountToFund <= 0)
            return new FundDreamServiceResult(FundDreamServiceStatus.InvalidAmount, "Amount must be positive.");

        var (ctx, error) = await GetFundingContext(dreamId, userId);
        if (error != null) return error;

        if (ctx!.Wallet.Balance < amountToFund)
            return new FundDreamServiceResult(FundDreamServiceStatus.InsufficientFunds, "Insufficient funds.");

        var existingFund = await context.Fund
            .SingleOrDefaultAsync(f => f.DreamID == dreamId &&
                                       f.UserID == userId &&
                                       f.FundingRoundID == ctx.ActiveFundingRound.ID);

        var currentAmountFunded = existingFund?.Amount ?? 0m;
        if (currentAmountFunded + amountToFund > ctx.ActiveFundingRound.MaxPerDream)
            return new FundDreamServiceResult(
                FundDreamServiceStatus.ExceedsRoundLimit,
                $"Exceeds round limit ({ctx.ActiveFundingRound.MaxPercentagePerDream}%). Already funded: {currentAmountFunded:C}.");

        if (existingFund != null)
            existingFund.Amount += amountToFund;
        else
            context.Fund.Add(new Fund
            {
                DreamID = dreamId,
                UserID = userId,
                Amount = amountToFund,
                FundingRoundID = ctx.ActiveFundingRound.ID
            });

        ctx.Wallet.Balance -= amountToFund;
        await context.SaveChangesAsync();
        return new FundDreamServiceResult(FundDreamServiceStatus.Success);
    }

    public bool DreamExists(int id, int cloudID)
    {
        return context.Dream.Any(e => e.ID == id && e.CloudID == cloudID);
    }

    public async Task<bool> CheckIfUserCanEditDreamers(IEnumerable<IDreamersForAuthorization> dreamers, ClaimsPrincipal user)
    {
        var authorizationResult = await authorizationService.AuthorizeAsync(user, dreamers, Operations.EditDreamers);
        return authorizationResult.Succeeded;
    }

    private async Task<(FundingContext? Context, FundDreamServiceResult? Error)> GetFundingContext(
       int dreamId, string userId)
    {
        var dream = await GetQuick(dreamId);
        if (dream == null || !dream.IsOpenForFunding())
            return (null, new FundDreamServiceResult(FundDreamServiceStatus.CantFundDream,
                "Dream not found or not fundable."));

        var activeFundingRound = await fundingRoundService.GetActiveRound(cloudNameService.CloudID);
        if (activeFundingRound == null)
            return (null, new FundDreamServiceResult(FundDreamServiceStatus.NoActiveFundingRound,
                "There is no active funding round now."));

        var user = await userManager.FindByIdAsync(userId);
        var hasJoinedCloud = await cloudService.UserHasJoinedCloud(user!);
        if (!hasJoinedCloud)
            return (null, new FundDreamServiceResult(FundDreamServiceStatus.CantFundDream,
                "User cannot fund this dream."));

        // TODO : Check if user is an active BL-member
        var isActiveMember = await membershipCheckService.IsActiveMember(user!.Email!);
        if (!isActiveMember)
            return (null, new FundDreamServiceResult(FundDreamServiceStatus.CantFundDream,
                "Only active Borderland members can fund dreams. Please contact support if you think this is an error."));

        var wallet = user!.Wallets.SingleOrDefault(w => w.CloudID == cloudNameService.CloudID);
        if (wallet == null)
            return (null, new FundDreamServiceResult(FundDreamServiceStatus.WalletNotFound,
                "User wallet not found."));

        return (new FundingContext(user, wallet, activeFundingRound), null);
    }
}