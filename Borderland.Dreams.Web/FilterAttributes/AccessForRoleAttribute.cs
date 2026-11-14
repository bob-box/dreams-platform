using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Borderland.Dreams.FilterAttributes;

/* 
Isnt this the same as doing [Authorize(Roles = role)] on the controller/action? 
Yes, but this allows us to check for a role that is not necessarily the same as the user's current roles.
For example, we can check if the user has access to a specific cloud by checking for a claim like "CloudAccess:CloudID".
This is useful for actions that are not necessarily restricted to admins, but require access to a specific cloud.
*/
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