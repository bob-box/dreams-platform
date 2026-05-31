using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;
using Borderland.Dreams.Enums;

namespace Borderland.Dreams.Authorization.Dream;

/// <summary>
/// Authorization handler for the Publish operation.
/// Allows dreamers to publish their own Draft dreams.
/// </summary>
public class UserCanPublishDreamAuthorizationHandler(UserManager<User> userManager)
    : AuthorizationHandler<OperationAuthorizationRequirement, DreamViewModel>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
                           OperationAuthorizationRequirement requirement,
                           DreamViewModel dream)
    {
        // Only handle Publish operations
        if (requirement.Name != Constants.PUBLISH)
        {
            return Task.CompletedTask;
        }

        if (context.User == null || dream.Dreamers == null || dream.Dreamers.Count == 0)
        {
            return Task.CompletedTask;
        }

        // Only allow publishing if the dream is in Draft status
        if (dream.Status != DreamStatus.Draft)
        {
            return Task.CompletedTask;
        }

        // Check if the user is a main dreamer
        var mainDreamerUserId = dream.Dreamers.FirstOrDefault(x => x.IsMainDreamer)?.UserID;
        if (mainDreamerUserId == userManager.GetUserId(context.User))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
