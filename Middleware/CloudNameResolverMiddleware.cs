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
        resolver.CloudName = httpContext.Request.RouteValues["cloudName"]?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(resolver.CloudName))
        {
            resolver.CloudID = -1;
            await _next(httpContext);
            return;
        }

        if (cloudCache.TryGet(resolver.CloudName, out int cloudID))
        {
            resolver.CloudID = cloudID;
            await _next(httpContext);
            return;
        }

        var id = context.Cloud.SingleOrDefault(x => x.Name == resolver.CloudName)?.ID;
        if (id == null)
        {
            resolver.CloudID = -1;
            httpContext.Response.Redirect("/");
            await _next(httpContext);
            return;
        }

        cloudCache.Set(resolver.CloudName, id.Value);
        resolver.CloudID = id.Value;
        await _next(httpContext);
    }
}

public class CloudNameResolver : ICloudNameResolver
{
    public string CloudName { get; set; } = "";
    public int CloudID { get; set; } = -1;
}

public interface ICloudNameResolver
{
    public string CloudName { get; set; }
    public int CloudID { get; set; }
}