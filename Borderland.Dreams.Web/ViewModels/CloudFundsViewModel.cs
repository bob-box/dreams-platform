namespace Borderland.Dreams.ViewModels;

public class CloudFundsViewModel
{
    public decimal UserWalletBalance { get; set; }
    
    public List<FundingRoundViewModel> FundingRounds { get; set; }
 
    public DateTime? DreamingPeriodStart { get; internal set; }
    public DateTime? DreamingPeriodEnd { get; internal set; }
    public DateTime? DreamEditDeadline { get; internal set; }
}