using Borderland.Dreams.Data;
using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Middleware;
using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Borderland.Dreams.Enums;

namespace Borderland.Dreams.Services;

public class DreamFundingService(
    ApplicationDbContext context,
    ICloudNameResolver cloudNameService,
    IFundingRoundService fundingRoundService,
    UserManager<User> userManager,
    ICloudService cloudService,
    IMembershipCheckService membershipCheckService
) : IDreamFundingService
{
    public async Task<FundDreamServiceResult> GetFundingModal(int dreamId, string userId)
    {
        var (ctx, error) = await GetFundingContext(dreamId, userId);
        if (error != null) return error;

        var userFundsForThisDream = await context.Fund
            .AsNoTracking()
            .Where(f => f.DreamID == dreamId && f.UserID == userId)
            .ToListAsync();

        decimal userFundedInCurrentRound = userFundsForThisDream
            .SingleOrDefault(f => f.FundingRoundID == ctx!.ActiveFundingRound.ID)?.Amount ?? 0m;
        decimal userTotalFundedAllRounds = userFundsForThisDream.Sum(f => f.Amount);

        var viewModel = new FundingModalViewModel
        {
            DreamId = dreamId,
            RoundMaxAmount = ctx!.ActiveFundingRound.MaxPerDream,
            UserFundedInCurrentRound = userFundedInCurrentRound,
            UserTotalFundedAllRounds = userTotalFundedAllRounds,
            UserWalletBalance = ctx!.Wallet.Balance
        };

        return new FundDreamServiceResult(FundDreamServiceStatus.Success, ViewModel: viewModel);
    }

    public async Task<FundDreamServiceResult> FundDream(int dreamId, decimal amountToFund, string userId)
    {
        if (amountToFund <= 0)
            return new FundDreamServiceResult(FundDreamServiceStatus.InvalidAmount, "Amount must be positive.");

        var (ctx, error) = await GetFundingContext(dreamId, userId);
        if (error != null) return error;

        // Hold the SQLite writer lock while checking current balances and recording the contribution.
        // Membership verification above can call an external service and must finish before taking the lock.
        await using var transaction = await context.Database.BeginTransactionAsync();
        var (round, roundError) = await GetFundableRound(dreamId);
        if (roundError != null) return roundError;

        var activeRound = round!;
        if (activeRound.ID != ctx!.ActiveFundingRound.ID)
            return new FundDreamServiceResult(FundDreamServiceStatus.NoActiveFundingRound,
                "The funding round changed. Please try again.");

        var wallet = ctx.Wallet;
        await context.Entry(wallet).ReloadAsync();
        if (context.Entry(wallet).State == EntityState.Detached)
            return new FundDreamServiceResult(FundDreamServiceStatus.WalletNotFound, "User wallet not found.");

        if (wallet.Balance < amountToFund)
            return new FundDreamServiceResult(FundDreamServiceStatus.InsufficientFunds, "Insufficient funds.");

        var existingFund = await context.Fund
            .SingleOrDefaultAsync(f => f.DreamID == dreamId &&
                                       f.UserID == userId &&
                                       f.FundingRoundID == activeRound.ID);

        var currentAmountFunded = existingFund?.Amount ?? 0m;
        if (currentAmountFunded + amountToFund > activeRound.MaxPerDream)
            return new FundDreamServiceResult(
                FundDreamServiceStatus.ExceedsRoundLimit,
                $"Exceeds round limit ({activeRound.MaxPercentagePerDream}%). Already funded: {currentAmountFunded:C}.");

        if (existingFund != null)
            existingFund.Amount += amountToFund;
        else
            context.Fund.Add(new Fund
            {
                DreamID = dreamId,
                UserID = userId,
                Amount = amountToFund,
                FundingRoundID = activeRound.ID
            });

        wallet.Balance -= amountToFund;
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return new FundDreamServiceResult(FundDreamServiceStatus.Success);
    }

    private async Task<(FundingContext? Context, FundDreamServiceResult? Error)> GetFundingContext(
        int dreamId, string userId)
    {
        var (activeFundingRound, error) = await GetFundableRound(dreamId);
        if (error != null) return (null, error);

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
            return (null, new FundDreamServiceResult(FundDreamServiceStatus.CantFundDream,
                "User not found."));

        var hasJoinedCloud = await cloudService.UserHasJoinedCloud(user);
        if (!hasJoinedCloud)
            return (null, new FundDreamServiceResult(FundDreamServiceStatus.CantFundDream,
                "You must join the cloud to fund dreams."));

        // TODO : Check if user is an active BL-member.
        var isActiveMember = await membershipCheckService.IsActiveMember(user.Email!);
        if (!isActiveMember)
            return (null, new FundDreamServiceResult(FundDreamServiceStatus.CantFundDream,
                "Only active Borderland members can fund dreams. Please contact support if you think this is an error."));

        var wallet = user.Wallets.SingleOrDefault(w => w.CloudID == cloudNameService.CloudID);
        if (wallet == null)
            return (null, new FundDreamServiceResult(FundDreamServiceStatus.WalletNotFound,
                "User wallet not found."));

        return (new FundingContext(wallet, activeFundingRound!), null);
    }

    private async Task<(FundingRoundViewModel? Round, FundDreamServiceResult? Error)> GetFundableRound(int dreamId)
    {
        var isFundable = await context.Dream.AnyAsync(d =>
            d.ID == dreamId && d.CloudID == cloudNameService.CloudID && d.Status == DreamStatus.Active);
        if (!isFundable)
            return (null, new FundDreamServiceResult(FundDreamServiceStatus.CantFundDream,
                "Dream not found or not fundable."));

        var activeFundingRound = await fundingRoundService.GetActiveRound(cloudNameService.CloudID);
        if (activeFundingRound == null)
            return (null, new FundDreamServiceResult(FundDreamServiceStatus.NoActiveFundingRound,
                "There is no active funding round now."));

        return (activeFundingRound, null);
    }

    private sealed record FundingContext(
        Wallet Wallet,
        FundingRoundViewModel ActiveFundingRound);
}
