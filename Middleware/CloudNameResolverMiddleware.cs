using Borderland.Dreams.Data;

namespace Borderland.Dreams.Middleware;

public class CloudNameResolverMiddleware(RequestDelegate _next)
{
    private readonly Dictionary<string, int> _cloudCache = new(StringComparer.OrdinalIgnoreCase);

    public async Task InvokeAsync(HttpContext httpContext, ICloudNameResolver resolver, ApplicationDbContext context)
    {
        var cloudName = httpContext.Request.RouteValues["cloudName"]?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(cloudName))
        {
            resolver.CloudID = -1;
            await _next(httpContext);
            return;
        }
        // See if the CloudID is already cached
        if (_cloudCache.TryGetValue(cloudName, out int cloudID))
        {
            resolver.CloudName = cloudName;
            resolver.CloudID = cloudID;
            await _next(httpContext);
            return;
        }

        // Lookup in db so we could cache it
        var id = context.Cloud.SingleOrDefault(x => x.Name == cloudName)?.ID;
        if (id == null)
        {
            resolver.CloudID = -1;
            await _next(httpContext);
            return;
        }
        _cloudCache[cloudName] = id.Value;

        resolver.CloudID = id.Value;
        resolver.CloudName = cloudName;
        await _next(httpContext);
    }
}

public static class CloudNameResolverMiddlewareExtensions
{
    public static IApplicationBuilder UseCloudNameResolver(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CloudNameResolverMiddleware>();
    }
}

public class CloudNameResolver : ICloudNameResolver
{
    public string CloudName { get; set; } = "";
    public int CloudID { get; set; } = -1;
}

public interface ICloudNameResolver
{
    public string? CloudName { get; set; }
    public int CloudID { get; set; }
}