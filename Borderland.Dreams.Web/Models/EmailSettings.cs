using Borderland.Dreams.Interfaces;

namespace Borderland.Dreams.Models;

public class EmailSettings : IAutoPopulateCreatedAtOnAdd
{
    public int ID { get; set; } = 1;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    /// <summary>
    /// For one-to-one emails triggered by a user action (e.g. password resets).
    /// </summary>
    public string TransactionalMessageStream { get; set; } = "outbound";

    /// <summary>
    /// For one-to-many sends (e.g. timeline changes and general info)
    /// </summary>
    public string BroadcastMessageStream { get; set; } = "broadcast";
    public DateTime CreatedAtUtc { get; set; }
}
