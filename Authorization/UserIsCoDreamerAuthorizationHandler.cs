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
                return Task.CompletedTask;
            }

            // If not asking for read or update permission, return.
            if (requirement.Name != Constants.ReadOperationName &&
                requirement.Name != Constants.UpdateOperationName)
            {
                return Task.CompletedTask;
            }

            var coDreamerUserIds = dream.Dreamers.Where(x => !x.IsMainDreamer)
                                                 .Select(x => x.User.Id);

            if (coDreamerUserIds.Contains(_userManager.GetUserId(context.User)))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}