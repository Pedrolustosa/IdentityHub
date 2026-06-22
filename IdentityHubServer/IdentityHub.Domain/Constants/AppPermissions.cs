namespace IdentityHub.Domain.Constants
{
    public static class AppPermissions
    {
        public static IReadOnlyList<string> All() =>
        [
            Users.View,
            Users.Create,
            Users.Update,
            Users.Delete,
            Users.UpdateRoles,
            Users.InvitesView,

            Roles.View,
            Roles.Create,
            Roles.Update,
            Roles.Delete,
            Roles.PermissionsView,
            Roles.PermissionsUpdate,

            Dashboard.View,
            Sessions.View,
            Sessions.Revoke,
            Activity.View,
            Audit.View,
            SecurityEvents.View,
            SecurityEvents.Manage,
            SecuritySettings.View,
            SecuritySettings.Update,
            Permissions.CatalogView,
            Permissions.MatrixView,
            UserInvites.View,
            UserInvites.Create,
            UserInvites.Cancel,
            UserInvites.Resend
        ];

        public static class Users
        {
            public const string View = "Users.View";
            public const string Create = "Users.Create";
            public const string Update = "Users.Update";
            public const string Delete = "Users.Delete";
            public const string UpdateRoles = "Users.Roles.Update";
            public const string InvitesView = "Users.Invites.View";
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

        public static class Dashboard
        {
            public const string View = "Dashboard.View";
        }

        public static class Sessions
        {
            public const string View = "Sessions.View";
            public const string Revoke = "Sessions.Revoke";
        }

        public static class Activity
        {
            public const string View = "Activity.View";
        }

        public static class Audit
        {
            public const string View = "Audit.View";
        }

        public static class SecurityEvents
        {
            public const string View = "SecurityEvents.View";
            public const string Manage = "SecurityEvents.Manage";
        }

        public static class SecuritySettings
        {
            public const string View = "SecuritySettings.View";
            public const string Update = "SecuritySettings.Update";
        }

        public static class Permissions
        {
            public const string CatalogView = "Permissions.Catalog.View";
            public const string MatrixView = "Permissions.Matrix.View";
        }

        public static class UserInvites
        {
            public const string View = "UserInvites.View";
            public const string Create = "UserInvites.Create";
            public const string Cancel = "UserInvites.Cancel";
            public const string Resend = "UserInvites.Resend";
        }
    }
}
