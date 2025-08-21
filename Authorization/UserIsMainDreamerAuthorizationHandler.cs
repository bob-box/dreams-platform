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
                return Task.CompletedTask;
            }

            // If not asking for CRUD permission, return.
            if (requirement.Name != Constants.CreateOperationName &&
                requirement.Name != Constants.ReadOperationName &&
                requirement.Name != Constants.UpdateOperationName &&
                requirement.Name != Constants.DeleteOperationName)
            {
                return Task.CompletedTask;
            }

            var mainDreamerUserId = dream.Dreamers.Single(x => x.IsMainDreamer).UserID;
            if (mainDreamerUserId == _userManager.GetUserId(context.User))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
