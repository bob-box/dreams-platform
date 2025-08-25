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

    private Dictionary<string, int> _cloudCache = new Dictionary<string, int>();

    public int GetCloudID()
    {
        var cloudYear = HttpContext.Request.RouteValues["year"]?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(cloudYear))
        {
            throw new ArgumentNullException(nameof(cloudYear));
        }

        // See if the CloudID is already cached
        if (_cloudCache.TryGetValue(cloudYear, out var cloudID))
        {
            return cloudID;
        }

        // Lookup in db and cache it
        cloudID = _context.Cloud.SingleOrDefault(x => x.Name == cloudYear)?.ID ?? 0;
        _cloudCache[cloudYear] = cloudID;
        return cloudID;
    }
}
