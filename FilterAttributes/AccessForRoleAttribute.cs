using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Borderland.Dreams.FilterAttributes;

public class AccessForRoleAttribute(string role) : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var userIsInRole = context.HttpContext.User.IsInRole(role);
        if (!userIsInRole)
        {
            context.Result = new ForbidResult();
            return;
        }

        await next();
    }
}