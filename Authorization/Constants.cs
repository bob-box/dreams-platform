namespace Borderland.Dreams.Authorization
{
    public static class Constants
    {
        public static readonly string CREATE = "Create";
        public static readonly string READ = "Read";
        public static readonly string UPDATE = "Update";
        public static readonly string DELETE = "Delete";

        public const string AdminRole = "Admin";
        /// <summary>
        /// Role for users who have an active BL membership.
        /// </summary>
        public const string MemberRole = "Member";

        public const string JoinedCloudClaim = "JoinedCloud";
    }
}