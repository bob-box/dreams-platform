namespace Borderland.Dreams.ViewModels;

public class FundingModalViewModel
{
    public int DreamId { get; set; }
    public decimal RoundMaxAmount { get; set; }
    // amount user has already contributed to this dream in the current round
    public decimal UserFundedInCurrentRound { get; set; }
    public decimal UserTotalFundedAllRounds { get; set; }
    public decimal UserWalletBalance { get; set; }
}