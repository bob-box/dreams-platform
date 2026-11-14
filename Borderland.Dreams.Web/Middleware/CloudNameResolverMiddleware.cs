using Borderland.Dreams.Data;

namespace Borderland.Dreams.Middleware;

public class CloudNameResolverMiddleware(RequestDelegate _next)
{
    public async Task InvokeAsync(
        HttpContext httpContext,
        ICloudNameResolver resolver,
        ApplicationDbContext context,
        ICloudCache cloudCache
        )
    {
        resolver.CloudSlug = httpContext.Request.RouteValues["cloudSlug"]?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(resolver.CloudSlug))
        {
            resolver.CloudID = -1;
            resolver.CloudName = "";
            await _next(httpContext);
            return;
        }

        if (cloudCache.TryGet(resolver.CloudSlug, out int cloudID, out var cloudName))
        {
            resolver.CloudID = cloudID;
            resolver.CloudName = cloudName;
            await _next(httpContext);
            return;
        }

        var cloud = context.Cloud.SingleOrDefault(x => x.Slug == resolver.CloudSlug);
        if (cloud == null)
        {
            resolver.CloudID = -1;
            resolver.CloudName = "";
            httpContext.Response.Redirect("/");
            await _next(httpContext);
            return;
        }

        cloudCache.Set(resolver.CloudSlug, cloud.ID, cloud.Name);
        resolver.CloudID = cloud.ID;
        resolver.CloudName = cloud.Name;
        await _next(httpContext);
    }
}

public class CloudNameResolver : ICloudNameResolver
{
    public string CloudName { get; set; } = "";
    public string CloudSlug { get; set; } = "";
    public int CloudID { get; set; } = -1;
}

public interface ICloudNameResolver
{
    public string CloudName { get; set; }
    public string CloudSlug { get; set; }
    public int CloudID { get; set; }
}