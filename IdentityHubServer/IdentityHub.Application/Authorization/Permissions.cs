using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityHub.Application.Authorization
{
    public static class Permissions
    {
        public const string Users_View = "Users.View";
        public const string Users_Create = "Users.Create";
        public const string Users_Update = "Users.Update";
        public const string Users_Delete = "Users.Delete";

        public const string Roles_Manage = "Roles.Manage";
    }
}
