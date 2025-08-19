using Borderland.Dreams.Data;
using Microsoft.AspNetCore.Mvc;

namespace Borderland.Dreams.Controllers;

public class BaseController : Controller
{
    protected readonly ApplicationDbContext _context;
    private Dictionary<string, int> _cloudCache = new Dictionary<string, int>();

    public BaseController(ApplicationDbContext context)
    {
        _context = context;
    }

    public int GetCloudID()
    {
        var cloudYear = HttpContext.Request.RouteValues["year"]?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(cloudYear))
        {
            return 0;
        }

        // See if the CloudID is already cached
        if (_cloudCache.TryGetValue(cloudYear, out var cloudID))
        {
            return cloudID;
        }

        // Lookup in db and cache it
        cloudID = _context.Cloud.FirstOrDefault(x => x.Name == cloudYear)?.ID ?? 0;
        _cloudCache[cloudYear] = cloudID;
        return cloudID;
    }
}
