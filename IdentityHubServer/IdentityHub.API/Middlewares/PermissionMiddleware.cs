using System.Security.Claims;

namespace IdentityHub.API.Middlewares
{
    public class PermissionMiddleware
    {
        private readonly RequestDelegate _next;
        private const string PermissionClaimType = "permission";

        public PermissionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var endpoint = context.GetEndpoint();

            if (endpoint == null)
            {
                await _next(context);
                return;
            }

            var authorizeAttributes = endpoint.Metadata
                .GetOrderedMetadata<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>();

            if (authorizeAttributes == null || !authorizeAttributes.Any())
            {
                await _next(context);
                return;
            }

            var requiredPolicies = authorizeAttributes
                .Where(a => !string.IsNullOrWhiteSpace(a.Policy))
                .Select(a => a.Policy!)
                .ToList();

            if (!requiredPolicies.Any())
            {
                await _next(context);
                return;
            }

            var user = context.User;

            if (user?.Identity == null || !user.Identity.IsAuthenticated)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            var userPermissions = user.Claims
                .Where(c => c.Type == PermissionClaimType)
                .Select(c => c.Value)
                .ToHashSet();

            var hasAccess = requiredPolicies.Any(p => userPermissions.Contains(p));

            if (!hasAccess)
            {
                Console.WriteLine($"Access denied: {context.Request.Path}");
                Console.WriteLine($"Required: {string.Join(",", requiredPolicies)}");
                Console.WriteLine($"User: {string.Join(",", userPermissions)}");

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Forbidden");
                return;
            }

            await _next(context);
        }
    }
}