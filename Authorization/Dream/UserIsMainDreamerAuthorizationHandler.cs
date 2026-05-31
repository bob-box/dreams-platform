using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Authorization.Dream;

// Based on https://learn.microsoft.com/en-us/aspnet/core/security/authorization/secure-data?view=aspnetcore-8.0
public class UserIsMainDreamerAuthorizationHandler(UserManager<User> userManager)
            : AuthorizationHandler<OperationAuthorizationRequirement, DreamViewModel>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
                               OperationAuthorizationRequirement requirement,
                               DreamViewModel dream)
    {
        if (context.User == null || dream.Dreamers == null || dream.Dreamers.Count == 0)
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

        // You can always READ your own dream
        var mainDreamerUserId = dream.Dreamers.Single(x => x.IsMainDreamer).UserID;
        if (mainDreamerUserId != userManager.GetUserId(context.User) &&
            requirement.Name == Constants.READ &&
            !dream.IsVisibleToPublic())
        {
            return Task.CompletedTask;
        }

        // UPDATE is only allowed when the dream is editable.
        // All other allowed operations are always permitted for the main dreamer.
        if ((
            requirement.Name == Constants.UPDATE ||
            requirement.Name == Constants.EDIT_DREAMER
            ) &&
            !dream.IsEditable())
        {
            return Task.CompletedTask;
        }

        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
