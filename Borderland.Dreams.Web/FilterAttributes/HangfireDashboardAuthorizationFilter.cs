using Borderland.Dreams.Authorization;
using Hangfire.Dashboard;

namespace Borderland.Dreams.FilterAttributes;

public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    // Hangfire dashboard authorization: only Admin role can access
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true &&
                httpContext.User.IsInRole(Constants.AdminRole);
    }
}