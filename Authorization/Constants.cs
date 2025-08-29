namespace Borderland.Dreams.Authorization
{
    public static class Constants
    {
        public static readonly string CREATE = "Create";
        public static readonly string READ = "Read";
        public static readonly string UPDATE = "Update";
        public static readonly string DELETE = "Delete";

        public static readonly string AdministratorsRole = "Admin";
        /// <summary>
        /// Role for users who have an active BL membership.
        /// </summary>
        public static readonly string MemberRole = "Member";
    }
}