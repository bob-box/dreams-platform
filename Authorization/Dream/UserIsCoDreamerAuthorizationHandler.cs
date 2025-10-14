using Borderland.Dreams.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Identity;

namespace Borderland.Dreams.Authorization.Dream;

// Based on https://learn.microsoft.com/en-us/aspnet/core/security/authorization/secure-data?view=aspnetcore-8.0
public class UserIsCoDreamerAuthorizationHandler(UserManager<User> userManager) :
    AuthorizationHandler<OperationAuthorizationRequirement, IDreamWithDreamersForAuthorization>
{
    UserManager<User> _userManager = userManager;

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
                               OperationAuthorizationRequirement requirement,
                               IDreamWithDreamersForAuthorization dream)
    {
        if (context.User == null || dream == null)
        {
            // Returning Task.CompletedTask without a prior call to context.Success or context.Fail
            // is not a success or failure, it allows other authorization handlers to run.
            // If you need to explicitly fail, call context.Fail.
            return Task.CompletedTask;
        }

        var allowedOperations = new string[]
        {
            Constants.READ,
            Constants.UPDATE
        };

        // If not asking for the allowed operations, return.
        if (!allowedOperations.Contains(requirement.Name))
        {
            return Task.CompletedTask;
        }

        var coDreamerUserIds = dream.Dreamers.Where(x => !x.IsMainDreamer)
                                             .Select(x => x.UserID);

        if (coDreamerUserIds.Contains(_userManager.GetUserId(context.User)))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}