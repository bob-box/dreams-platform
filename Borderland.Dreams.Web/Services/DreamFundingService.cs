using Borderland.Dreams.Data;
using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Middleware;
using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;
using Microsoft.EntityFrameworkCore;
using Borderland.Dreams.Enums;

namespace Borderland.Dreams.Services;

public class DreamFundingService(
    ApplicationDbContext context,
    ICloudNameResolver cloudNameService,
    IAppTime appTime,
    ICloudService cloudService,
    IMembershipCheckService membershipCheckService
) : IDreamFundingService
{
    public async Task<FundDreamServiceResult> GetFundingModal(int dreamId, string userId)
    {
        var (round, error) = await GetFundableRound(dreamId);
        if (error != null) return error;
        var membershipError = await CheckMembership(userId);
        if (membershipError != null) return membershipError;

        var wallet = await WalletQuery(userId).AsNoTracking().SingleOrDefaultAsync();
        if (wallet == null) return WalletNotFound();

        var userFundsForThisDream = await context.Fund
            .AsNoTracking()
            .Where(f => f.DreamID == dreamId && f.UserID == userId)
            .ToListAsync();

        decimal userFundedInCurrentRound = userFundsForThisDream
            .SingleOrDefault(f => f.FundingRoundID == round!.ID)?.Amount ?? 0m;
        decimal userTotalFundedAllRounds = userFundsForThisDream.Sum(f => f.Amount);

        var viewModel = new FundingModalViewModel
        {
            DreamId = dreamId,
            RoundMaxAmount = GetRoundLimit(round!),
            UserFundedInCurrentRound = userFundedInCurrentRound,
            UserTotalFundedAllRounds = userTotalFundedAllRounds,
            UserWalletBalance = wallet.Balance
        };

        return new FundDreamServiceResult(FundDreamServiceStatus.Success, ViewModel: viewModel);
    }

    public async Task<FundDreamServiceResult> FundDream(int dreamId, decimal amountToFund, string userId)
    {
        if (amountToFund <= 0)
            return new FundDreamServiceResult(FundDreamServiceStatus.InvalidAmount, "Amount must be positive.");

        var (expectedRoundId, error) = await GetFundableRoundId(dreamId);
        if (error != null) return error;
        var membershipError = await CheckMembership(userId);
        if (membershipError != null) return membershipError;

        // Hold the SQLite writer lock while checking current balances and recording the contribution.
        // Membership verification above can call an external service and must finish before taking the lock.
        await using var transaction = await context.Database.BeginTransactionAsync();
        var (round, roundError) = await GetFundableRound(dreamId);
        if (roundError != null) return roundError;

        var activeRound = round!;
        if (activeRound.ID != expectedRoundId)
            return new FundDreamServiceResult(FundDreamServiceStatus.NoActiveFundingRound,
                "The funding round changed. Please try again.");

        var wallet = await WalletQuery(userId).SingleOrDefaultAsync();
        if (wallet == null) return WalletNotFound();

        if (wallet.Balance < amountToFund)
            return new FundDreamServiceResult(FundDreamServiceStatus.InsufficientFunds, "Insufficient funds.");

        var existingFund = await context.Fund
            .SingleOrDefaultAsync(f => f.DreamID == dreamId &&
                                       f.UserID == userId &&
                                       f.FundingRoundID == activeRound.ID);

        var currentAmountFunded = existingFund?.Amount ?? 0m;
        if (currentAmountFunded + amountToFund > GetRoundLimit(activeRound))
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

    private async Task<FundDreamServiceResult?> CheckMembership(string userId)
    {
        var user = await context.Users.IgnoreAutoIncludes().AsNoTracking().SingleOrDefaultAsync(user => user.Id == userId);
        if (user == null)
            return new FundDreamServiceResult(FundDreamServiceStatus.CantFundDream,
                "User not found.");

        var hasJoinedCloud = await cloudService.UserHasJoinedCloud(user);
        if (!hasJoinedCloud)
            return new FundDreamServiceResult(FundDreamServiceStatus.CantFundDream,
                "You must join the cloud to fund dreams.");

        var isActiveMember = await membershipCheckService.IsActiveMember(user.Email!);
        if (!isActiveMember)
            return new FundDreamServiceResult(FundDreamServiceStatus.CantFundDream,
                "Only active Borderland members can fund dreams. Please contact support if you think this is an error.");

        return null;
    }

    private IQueryable<Wallet> WalletQuery(string userId) =>
        context.Wallet.Where(wallet => wallet.UserID == userId && wallet.CloudID == cloudNameService.CloudID);

    private static FundDreamServiceResult WalletNotFound() =>
        new(FundDreamServiceStatus.WalletNotFound, "User wallet not found.");

    private static decimal GetRoundLimit(FundingRound round) =>
        round.AllocationPerUser * (round.MaxPercentagePerDream / 100m);

    private async Task<(int? RoundId, FundDreamServiceResult? Error)> GetFundableRoundId(int dreamId)
    {
        var (round, error) = await GetFundableRound(dreamId);
        return (round?.ID, error);
    }

    private async Task<(FundingRound? Round, FundDreamServiceResult? Error)> GetFundableRound(int dreamId)
    {
        var isFundable = await context.Dream.AnyAsync(d =>
            d.ID == dreamId && d.CloudID == cloudNameService.CloudID && d.Status == DreamStatus.Active);
        if (!isFundable)
            return (null, new FundDreamServiceResult(FundDreamServiceStatus.CantFundDream,
                "Dream not found or not fundable."));

        var activeFundingRound = await FundingRoundService.ActiveRoundQuery(context, cloudNameService.CloudID, appTime.UtcNow)
            .SingleOrDefaultAsync();
        if (activeFundingRound == null)
            return (null, new FundDreamServiceResult(FundDreamServiceStatus.NoActiveFundingRound,
                "There is no active funding round now."));

        return (activeFundingRound, null);
    }
}
