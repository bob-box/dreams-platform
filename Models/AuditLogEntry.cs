using Borderland.Dreams.Interfaces;

namespace Borderland.Dreams.Models;

public class AuditLogEntry : IAutoPopulateCreatedAtOnAdd
{
    public int ID { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CloudID { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int EntityID { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? UserID { get; set; }
    public string UserName { get; set; } = "System";
    public string Changes { get; set; } = "{}";
}
