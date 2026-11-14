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
            Constants.UPDATE,
            Constants.PUBLISH,
            Constants.RETURN_TO_DRAFT
        };

        // If not asking for the allowed operations, return.
        if (!allowedOperations.Contains(requirement.Name))
        {
            return Task.CompletedTask;
        }

        var currentUserId = userManager.GetUserId(context.User);
        var isCoDreamer = dream.Dreamers.Any(x =>
            !x.IsMainDreamer && x.UserID == currentUserId);

        // Private dreams are only visible to their dreamers.
        if (requirement.Name == Constants.READ &&
            !isCoDreamer &&
            !dream.IsVisibleToPublic())
        {
            return Task.CompletedTask;
        }

        // Co-dreamers may submit or return their dream to draft; the service enforces state and deadline rules.
        if ((requirement.Name == Constants.PUBLISH ||
             requirement.Name == Constants.RETURN_TO_DRAFT) &&
            !isCoDreamer)
            return Task.CompletedTask;

        // UPDATE is only allowed when the dream is editable.
        if (requirement.Name == Constants.UPDATE &&
            (!isCoDreamer || !dream.IsEditable()))
            return Task.CompletedTask;

        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}