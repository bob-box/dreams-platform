using Borderland.Dreams.Data;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Borderland.Dreams.Middleware;

public class CloudNameResolverActionFilter : IActionFilter
{
    private readonly ICloudNameResolver _cloudNameResolver;
    private readonly ApplicationDbContext _dbContext;
    private Dictionary<string, int> _cloudCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    public CloudNameResolverActionFilter(ICloudNameResolver cloudNameResolver, ApplicationDbContext dbContext)
    {
        _cloudNameResolver = cloudNameResolver;
        _dbContext = dbContext;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        // This runs BEFORE the action method
        Console.WriteLine("Before action execution");

        var cloudName = context.HttpContext.Request.RouteValues["cloudName"]?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(cloudName))
        {
            _cloudNameResolver.CloudID = -1;
            return;
        }
        // See if the CloudID is already cached
        if (_cloudCache.TryGetValue(cloudName, out int cloudID))
        {
            _cloudNameResolver.CloudName = cloudName;
            _cloudNameResolver.CloudID = cloudID;
            return;
        }

        // Lookup in db and cache it
        var id = _dbContext.Cloud.SingleOrDefault(x => x.Name == cloudName)?.ID;
        if (id == null)
        {
            _cloudNameResolver.CloudID = -1;
            return;
        }

        _cloudCache[cloudName] = id.Value;
        _cloudNameResolver.CloudID = id.Value;
        _cloudNameResolver.CloudName = cloudName;

    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        // This runs AFTER the action method
        Console.WriteLine("After action execution");
    }
}