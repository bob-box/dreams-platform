using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Interfaces;

public interface IAuditLogService
{
    Task<AuditLogViewModel> GetAuditLog(
        string? searchText,
        string? entityType,
        string? action,
        int? cloudId,
        DateTime? from,
        DateTime? to);
}
