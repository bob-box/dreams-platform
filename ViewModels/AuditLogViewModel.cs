namespace Borderland.Dreams.ViewModels;

public class AuditLogViewModel
{
    public string? SearchText { get; set; }
    public string? EntityType { get; set; }
    public string? Action { get; set; }
    public int? CloudID { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public IReadOnlyList<AuditLogCloudOption> Clouds { get; set; } = [];
    public IReadOnlyList<AuditLogItemViewModel> Entries { get; set; } = [];
}

public record AuditLogCloudOption(int ID, string Name);

public class AuditLogItemViewModel
{
    public DateTime CreatedAt { get; set; }
    public int CloudID { get; set; }
    public string CloudName { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int EntityID { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public IReadOnlyList<AuditLogChangeViewModel> Changes { get; set; } = [];
}

public record AuditLogChangeViewModel(string Property, string? OldValue, string? NewValue);
