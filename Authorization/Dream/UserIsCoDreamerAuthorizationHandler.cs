using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Identity;

namespace Borderland.Dreams.Authorization.Dream;

// Based on https://learn.microsoft.com/en-us/aspnet/core/security/authorization/secure-data?view=aspnetcore-8.0
public class UserIsCoDreamerAuthorizationHandler(UserManager<User> userManager) :
    AuthorizationHandler<OperationAuthorizationRequirement, DreamViewModel>
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
        // You can always READ your own dream
        if (!coDreamerUserIds.Contains(userManager.GetUserId(context.User)) &&
            requirement.Name == Constants.READ &&
            !dream.IsVisibleToPublic())
            return Task.CompletedTask;

        // UPDATE is only allowed when the dream is editable.
        if (requirement.Name == Constants.UPDATE && !dream.IsEditable())
            return Task.CompletedTask;

        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}