using Borderland.Dreams.Authorization;
using Borderland.Dreams.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Borderland.Dreams.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Constants.AdminRole)]
public class AuditLogController(IAuditLogService auditLogService) : Controller
{
    public async Task<IActionResult> Index(
        string? searchText,
        string? entityType,
        [FromQuery(Name = "action")] string? actionFilter,
        int? cloudId,
        DateTime? from,
        DateTime? to)
    {
        var viewModel = await auditLogService.GetAuditLog(searchText, entityType, actionFilter, cloudId, from, to);
        return View(viewModel);
    }
}
