using IdentityHub.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace IdentityHub.Infrastructure.Data.Seed
{
    public static class UserSeed
    {
        public static async Task SeedAsync(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            string[] roles = { "Admin", "Manager", "User" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            await SeedPermissions(roleManager);

            await CreateUserIfNotExists(
                userManager, roleManager,
                "admin@identityhub.com",
                "Admin User",
                "Admin@123",
                "Admin");

            await CreateUserIfNotExists(
                userManager, roleManager,
                "manager@identityhub.com",
                "Manager User",
                "Manager@123",
                "Manager");

            await CreateUserIfNotExists(
                userManager, roleManager,
                "user@identityhub.com",
                "Normal User",
                "User@123",
                "User");
        }

        private static async Task SeedPermissions(RoleManager<IdentityRole> roleManager)
        {
            await AddPermission(roleManager, "Admin", "Users.View");
            await AddPermission(roleManager, "Admin", "Users.Create");
            await AddPermission(roleManager, "Admin", "Users.Update");
            await AddPermission(roleManager, "Admin", "Users.Delete");
            await AddPermission(roleManager, "Admin", "Roles.View");
            await AddPermission(roleManager, "Admin", "Roles.Manage");

            await AddPermission(roleManager, "Manager", "Users.View");
            await AddPermission(roleManager, "Manager", "Users.Update");

            await AddPermission(roleManager, "User", "Users.View");
        }

        private static async Task AddPermission(
            RoleManager<IdentityRole> roleManager,
            string roleName,
            string permission)
        {
            var role = await roleManager.FindByNameAsync(roleName);

            if (role == null)
                return;

            var claims = await roleManager.GetClaimsAsync(role);

            if (!claims.Any(c => c.Type == "permission" && c.Value == permission))
            {
                await roleManager.AddClaimAsync(
                    role,
                    new Claim("permission", permission));
            }
        }

        private static async Task CreateUserIfNotExists(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            string email,
            string name,
            string password,
            string role)
        {
            var user = await userManager.FindByEmailAsync(email);

            if (user != null)
                return;

            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = name,
                IsActive = true,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, password);

            if (!result.Succeeded)
                throw new Exception($"Error creating user {email}");

            await userManager.AddToRoleAsync(user, role);
        }
    }
}
