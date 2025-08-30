using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Borderland.Dreams.FilterAttributes;

public class AccessForRoleAttribute(string role) : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var isAdmin = context.HttpContext.User.IsInRole(role);
        if (!isAdmin)
        {
            context.Result = new ForbidResult();
            return;
        }

        await next();
    }
}