using Borderland.Dreams.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Identity;

namespace Borderland.Dreams.Authorization
{
    public class UserIsCoDreamerAuthorizationHandler(UserManager<User> userManager) :
        AuthorizationHandler<OperationAuthorizationRequirement, Dream>
    {
        UserManager<User> _userManager = userManager;

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
                                   OperationAuthorizationRequirement requirement,
                                   Dream dream)
        {
            if (context.User == null || dream == null)
            {
                // Returning Task.CompletedTask without a prior call to context.Success or context.Fail
                // is not a success or failure, it allows other authorization handlers to run.
                // If you need to explicitly fail, call context.Fail.
                return Task.CompletedTask;
            }

            // If not asking for the allowed operations, return.
            if (requirement.Name != Constants.ReadOperationName &&
                requirement.Name != Constants.UpdateOperationName)
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
}