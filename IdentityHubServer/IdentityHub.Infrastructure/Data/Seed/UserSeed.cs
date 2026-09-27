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

        private sealed record SeedUserProfile(
            string Email,
            string FullName,
            string Password,
            string Role,
            string PhoneNumber,
            DateOnly DateOfBirth);

        /// <summary>
        /// Development users with distinct country dial codes so the phone picker
        /// resolves a different flag for each account.
        /// </summary>
        private static readonly SeedUserProfile[] SeedUsers =
        [
            new(
                Email: "admin@identityhub.com",
                FullName: "Admin User",
                Password: "Admin@123",
                Role: "Admin",
                PhoneNumber: "+5511987654321", // Brazil (+55)
                DateOfBirth: new DateOnly(1985, 3, 15)),
            new(
                Email: "manager@identityhub.com",
                FullName: "Manager User",
                Password: "Manager@123",
                Role: "Manager",
                PhoneNumber: "+12125550199", // United States (+1)
                DateOfBirth: new DateOnly(1990, 7, 22)),
            new(
                Email: "user@identityhub.com",
                FullName: "Normal User",
                Password: "User@123",
                Role: "User",
                PhoneNumber: "+351912345678", // Portugal (+351)
                DateOfBirth: new DateOnly(1995, 11, 8))
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
                    AppPermissions.Sessions.Revoke,
                    AppPermissions.Activity.View,
                    AppPermissions.Audit.View,
                    AppPermissions.SecurityEvents.View,
                    AppPermissions.SecurityEvents.Manage,
                    AppPermissions.SecuritySettings.View,
                    AppPermissions.SecuritySettings.Update,
                    AppPermissions.Permissions.CatalogView,
                    AppPermissions.Permissions.MatrixView,
                    AppPermissions.UserInvites.View,
                    AppPermissions.UserInvites.Create,
                    AppPermissions.UserInvites.Cancel,
                    AppPermissions.UserInvites.Resend
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
                    AppPermissions.Activity.View,
                    AppPermissions.Permissions.CatalogView,
                    AppPermissions.Permissions.MatrixView,
                    AppPermissions.UserInvites.View,
                    AppPermissions.UserInvites.Create,
                    AppPermissions.UserInvites.Resend
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
            foreach (var profile in SeedUsers)
            {
                await EnsureUserAsync(userManager, profile);
            }
        }

        private static async Task EnsureUserAsync(
            UserManager<ApplicationUser> userManager,
            SeedUserProfile profile)
        {
            var existing = await userManager.FindByEmailAsync(profile.Email);

            if (existing is null)
            {
                var user = new ApplicationUser
                {
                    UserName = profile.Email,
                    Email = profile.Email,
                    FullName = profile.FullName,
                    PhoneNumber = profile.PhoneNumber,
                    DateOfBirth = profile.DateOfBirth,
                    IsActive = true,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(user, profile.Password);

                if (!result.Succeeded)
                    throw new Exception($"Error creating user {profile.Email}");

                await userManager.AddToRoleAsync(user, profile.Role);
                return;
            }

            // Idempotent backfill for databases seeded before phone / date-of-birth existed.
            var needsUpdate = false;

            if (string.IsNullOrWhiteSpace(existing.FullName))
            {
                existing.FullName = profile.FullName;
                needsUpdate = true;
            }

            if (string.IsNullOrWhiteSpace(existing.PhoneNumber))
            {
                existing.PhoneNumber = profile.PhoneNumber;
                needsUpdate = true;
            }

            if (existing.DateOfBirth is null)
            {
                existing.DateOfBirth = profile.DateOfBirth;
                needsUpdate = true;
            }

            if (needsUpdate)
            {
                var updateResult = await userManager.UpdateAsync(existing);
                if (!updateResult.Succeeded)
                    throw new Exception($"Error updating seeded profile for {profile.Email}");
            }
        }
    }
}
