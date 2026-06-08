namespace IdentityHub.Domain.Constants
{
    public static class AppPermissions
    {
        public static class Users
        {
            public const string View = "Users.View";
            public const string Create = "Users.Create";
            public const string Update = "Users.Update";
            public const string Delete = "Users.Delete";
            public const string UpdateRoles = "Users.Roles.Update";
        }

        public static class Roles
        {
            public const string View = "Roles.View";
            public const string Create = "Roles.Create";
            public const string Update = "Roles.Update";
            public const string Delete = "Roles.Delete";
            public const string PermissionsView = "Roles.Permissions.View";
            public const string PermissionsUpdate = "Roles.Permissions.Update";
        }

        public static class RoleClaims
        {
            public const string View = "RoleClaims.View";
            public const string Manage = "RoleClaims.Manage";
        }

        public static class Dashboard
        {
            public const string View = "Dashboard.View";
        }

        public static class Audit
        {
            public const string View = "Audit.View";
        }
    }
}
