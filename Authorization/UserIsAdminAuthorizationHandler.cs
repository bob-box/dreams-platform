using System.Threading.Tasks;
using Borderland.Dreams.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Borderland.Dreams.Authorization
{
    public class UserIsAdminAuthorizationHandler
        : AuthorizationHandler<OperationAuthorizationRequirement, Dream>
    {
        protected override Task HandleRequirementAsync(
                                    AuthorizationHandlerContext context,
                                    OperationAuthorizationRequirement requirement, 
                                    Dream dream)
        {
            if (context.User == null)
            {
                // Returning Task.CompletedTask without a prior call to context.Success or context.Fail
                // is not a success or failure, it allows other authorization handlers to run.
                // If you need to explicitly fail, call context.Fail.
                return Task.CompletedTask;
            }

            // Administrators can do anything.
            if (context.User.IsInRole(Constants.AdministratorsRole))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}