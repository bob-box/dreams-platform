using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Borderland.Dreams.Models;

namespace Borderland.Dreams.Authorization
{
    public class UserIsMainDreamerAuthorizationHandler(UserManager<User> userManager)
                : AuthorizationHandler<OperationAuthorizationRequirement, Dream>
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
            if (requirement.Name != Constants.CreateOperationName &&
                requirement.Name != Constants.ReadOperationName &&
                requirement.Name != Constants.UpdateOperationName &&
                requirement.Name != Constants.DeleteOperationName)
            {
                return Task.CompletedTask;
            }

            // If you get "sequence contains no elements" error here, then you forgot to .Include(d => d.Dreamers) in the initial query
            // Because there cannot be a dream without a main dreamer.
            var mainDreamerUserId = dream.Dreamers.Single(x => x.IsMainDreamer).UserID;
            if (mainDreamerUserId == _userManager.GetUserId(context.User))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
