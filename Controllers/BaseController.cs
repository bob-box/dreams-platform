using Borderland.Dreams.Data;
using Borderland.Dreams.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Borderland.Dreams.Controllers;

public class BaseController(ApplicationDbContext context,
    IAuthorizationService authorizationService,
    UserManager<User> userManager) : Controller
{
    protected readonly ApplicationDbContext _context = context;
    protected IAuthorizationService _authorizationService { get; } = authorizationService;
    protected UserManager<User> _userManager { get; } = userManager;

    private Dictionary<string, int> _cloudCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Tries to get the Cloud ID from database using the route data. Caches the result.
    /// </summary>
    /// <param name="cloudID"></param>
    /// <returns></returns>
    public bool TryGetCloudID(out int cloudID)
    {
        // Perhaps put this in a action filter OnActionExecuting so it runs before every action?
        var cloudName = HttpContext.Request.RouteValues["cloudName"]?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(cloudName))
        {
            cloudID = -1;
            return false;
        }

        // See if the CloudID is already cached
        if (_cloudCache.TryGetValue(cloudName, out cloudID))
        {
            return true;
        }

        // Lookup in db and cache it
        var id = _context.Cloud.SingleOrDefault(x => x.Name == cloudName)?.ID;
        if (id == null)
        {
            cloudID = -1;
            return false;
        }
        _cloudCache[cloudName] = id.Value;

        cloudID = id.Value;
        return true;
    }
}
