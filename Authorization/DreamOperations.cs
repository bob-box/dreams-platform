using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Borderland.Dreams.Authorization
{
    public static class DreamOperations
    {
        public static OperationAuthorizationRequirement Create = new() { Name = Constants.CREATE };
        public static OperationAuthorizationRequirement Read = new() { Name = Constants.READ };
        public static OperationAuthorizationRequirement Update = new() { Name = Constants.UPDATE };
        public static OperationAuthorizationRequirement Delete = new() { Name = Constants.DELETE };
    }

    public static class Constants
    {
        public static readonly string CREATE = "Create";
        public static readonly string READ = "Read";
        public static readonly string UPDATE = "Update";
        public static readonly string DELETE = "Delete";

        public static readonly string AdministratorsRole = "Admin";
        public static readonly string ManagersRole = "Manager";
    }
}