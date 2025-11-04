using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Borderland.Dreams.Models;

namespace Borderland.Dreams.Authorization.Dream;

// Based on https://learn.microsoft.com/en-us/aspnet/core/security/authorization/secure-data?view=aspnetcore-8.0
public class UserIsMainDreamerAuthorizationHandler(UserManager<User> userManager)
            : AuthorizationHandler<OperationAuthorizationRequirement, IEnumerable<IDreamersForAuthorization>>
{
    UserManager<User> _userManager = userManager;

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
                               OperationAuthorizationRequirement requirement,
                               IEnumerable<IDreamersForAuthorization> dreamers)
    {
        if (context.User == null || dreamers == null || !dreamers.Any())
        {
            // Returning Task.CompletedTask without a prior call to context.Success or context.Fail
            // is not a success or failure, it allows other authorization handlers to run.
            // If you need to explicitly fail, call context.Fail.
            return Task.CompletedTask;
        }

        var allowedOperations = new string[]
        {
            Constants.CREATE,
            Constants.READ,
            Constants.UPDATE,
            Constants.DELETE,
            Constants.EDIT_DREAMER
        };

        // If not asking for the allowed operations, return.
        if (!allowedOperations.Contains(requirement.Name))
        {
            return Task.CompletedTask;
        }

        var mainDreamerUserId = dreamers.Single(x => x.IsMainDreamer).UserID;
        if (mainDreamerUserId == _userManager.GetUserId(context.User))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
