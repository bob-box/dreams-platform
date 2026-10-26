namespace Borderland.Dreams.Models;

public class FundingRoundOutboxMessage
{
    public int ID { get; set; }
    public int FundingRoundID { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = "{}";
    public DateTime EnqueuedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}
