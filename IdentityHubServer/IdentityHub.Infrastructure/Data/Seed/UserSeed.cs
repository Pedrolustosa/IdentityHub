using IdentityHub.Domain.Constants;
using IdentityHub.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace IdentityHub.Infrastructure.Data.Seed
{
    public static class UserSeed
    {
        private const string PermissionClaimType = "permission";
        private static readonly string[] DeprecatedRolePermissions =
        [
            "RoleClaims.View",
            "RoleClaims.Manage"
        ];

        public static async Task SeedAsync(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            await EnsureRoles(roleManager);
            await EnsurePermissions(roleManager);
            await EnsureUsers(userManager);
        }

        public static async Task EnsureRolesAndPermissionsAsync(
            RoleManager<IdentityRole> roleManager)
        {
            await EnsureRoles(roleManager);
            await EnsurePermissions(roleManager);
        }

        private static async Task EnsureRoles(RoleManager<IdentityRole> roleManager)
        {
            string[] roles = { "Admin", "Manager", "User" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        private static async Task EnsurePermissions(RoleManager<IdentityRole> roleManager)
        {
            var rolePermissions = new Dictionary<string, List<string>>
            {
                ["Admin"] = new()
                {
                    AppPermissions.Users.View,
                    AppPermissions.Users.Create,
                    AppPermissions.Users.Update,
                    AppPermissions.Users.Delete,
                    AppPermissions.Users.UpdateRoles,
                    AppPermissions.Users.InvitesView,

                    AppPermissions.Roles.View,
                    AppPermissions.Roles.Create,
                    AppPermissions.Roles.Update,
                    AppPermissions.Roles.Delete,
                    AppPermissions.Roles.PermissionsView,
                    AppPermissions.Roles.PermissionsUpdate,

                    AppPermissions.Dashboard.View,
                    AppPermissions.Sessions.View,
                    AppPermissions.Activity.View,
                    AppPermissions.Audit.View,
                    AppPermissions.SecurityEvents.View,
                    AppPermissions.SecurityEvents.Manage,
                    AppPermissions.SecuritySettings.View
                },

                ["Manager"] = new()
                {
                    AppPermissions.Users.View,
                    AppPermissions.Users.Update,
                    AppPermissions.Users.InvitesView,

                    AppPermissions.Roles.View,
                    AppPermissions.Roles.PermissionsView,

                    AppPermissions.Dashboard.View,
                    AppPermissions.Sessions.View,
                    AppPermissions.Activity.View
                },

                ["User"] = new()
                {
                    AppPermissions.Users.View,

                    AppPermissions.Roles.View,
                    AppPermissions.Roles.PermissionsView
                }
            };

            foreach (var (roleName, permissions) in rolePermissions)
            {
                var role = await roleManager.FindByNameAsync(roleName);

                if (role == null)
                    continue;

                var existingClaims = await roleManager.GetClaimsAsync(role);

                foreach (var deprecatedPermission in DeprecatedRolePermissions)
                {
                    var deprecatedClaims = existingClaims
                        .Where(c => c.Type == PermissionClaimType && c.Value == deprecatedPermission)
                        .ToList();

                    foreach (var deprecatedClaim in deprecatedClaims)
                    {
                        await roleManager.RemoveClaimAsync(role, deprecatedClaim);
                    }
                }

                existingClaims = await roleManager.GetClaimsAsync(role);

                foreach (var permission in permissions)
                {
                    if (!existingClaims.Any(c =>
                        c.Type == PermissionClaimType &&
                        c.Value == permission))
                    {
                        await roleManager.AddClaimAsync(
                            role,
                            new Claim(PermissionClaimType, permission));
                    }
                }
            }
        }

        private static async Task EnsureUsers(
            UserManager<ApplicationUser> userManager)
        {
            await CreateUser(userManager, "admin@identityhub.com", "Admin User", "Admin@123", "Admin");
            await CreateUser(userManager, "manager@identityhub.com", "Manager User", "Manager@123", "Manager");
            await CreateUser(userManager, "user@identityhub.com", "Normal User", "User@123", "User");
        }

        private static async Task CreateUser(
            UserManager<ApplicationUser> userManager,
            string email,
            string name,
            string password,
            string role)
        {
            var existing = await userManager.FindByEmailAsync(email);

            if (existing != null)
                return;

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = name,
                IsActive = true,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(user, password);

            if (!result.Succeeded)
                throw new Exception($"Error creating user {email}");

            await userManager.AddToRoleAsync(user, role);
        }
    }
}