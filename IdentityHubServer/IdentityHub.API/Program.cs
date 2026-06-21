using IdentityHub.API.Authorization;
using IdentityHub.API.Middlewares;
using IdentityHub.Application.DTOs;
using IdentityHub.Domain.Entities;
using IdentityHub.Domain.Interfaces;
using IdentityHub.Infrastructure.Data;
using IdentityHub.Infrastructure.Data.Seed;
using IdentityHub.Infrastructure.Repositories;
using IdentityHub.Infrastructure.Security;
using IdentityHub.IoC;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

const string FrontendCorsPolicy = "FrontendPolicy";
const string AuthLoginRateLimitPolicy = "AuthLoginRateLimitPolicy";
const string AuthForgotPasswordRateLimitPolicy = "AuthForgotPasswordRateLimitPolicy";
const string AuthResendConfirmationRateLimitPolicy = "AuthResendConfirmationRateLimitPolicy";

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using Bearer scheme. Exemplo: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();

builder.Services.AddSingleton<IAuthorizationHandler, PermissionHandler>();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy(AuthLoginRateLimitPolicy, context =>
    {
        var configuration = context.RequestServices.GetRequiredService<IConfiguration>();

        var permitLimit = Math.Max(
            1,
            configuration.GetValue<int?>("RateLimiting:Auth:Login:PermitLimit") ?? 10);

        var windowSeconds = Math.Max(
            1,
            configuration.GetValue<int?>("RateLimiting:Auth:Login:WindowSeconds") ?? 60);

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });

    options.AddPolicy(AuthForgotPasswordRateLimitPolicy, context =>
    {
        var configuration = context.RequestServices.GetRequiredService<IConfiguration>();

        var permitLimit = Math.Max(
            1,
            configuration.GetValue<int?>("RateLimiting:Auth:ForgotPassword:PermitLimit") ?? 5);

        var windowSeconds = Math.Max(
            1,
            configuration.GetValue<int?>("RateLimiting:Auth:ForgotPassword:WindowSeconds") ?? 300);

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });

    options.AddPolicy(AuthResendConfirmationRateLimitPolicy, context =>
    {
        var configuration = context.RequestServices.GetRequiredService<IConfiguration>();

        var permitLimit = Math.Max(
            1,
            configuration.GetValue<int?>("RateLimiting:Auth:ResendConfirmation:PermitLimit") ?? 5);

        var windowSeconds = Math.Max(
            1,
            configuration.GetValue<int?>("RateLimiting:Auth:ResendConfirmation:WindowSeconds") ?? 300);

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});

builder.Services.Configure<SmtpSettings>(
    builder.Configuration.GetSection("Smtp"));

var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSettings["Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "JWT signing key is missing. Configure Jwt:Key via User Secrets or environment variables.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],

        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey))
    };

    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var rawSessionId = context.Principal?.FindFirst("sid")?.Value;

            if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(rawSessionId, out var sessionId))
            {
                context.Fail("Session information is missing from the token.");
                return;
            }

            var rawPermissionVersion = context.Principal?.FindFirst("permission_version")?.Value;

            if (!int.TryParse(rawPermissionVersion, out var tokenPermissionVersion))
            {
                context.Fail("Permission version is missing from the token.");
                return;
            }

            var dbContext = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();

            var isSessionActive = await dbContext.UserSessions
                .AsNoTracking()
                .AnyAsync(session =>
                    session.Id == sessionId &&
                    session.UserId == userId &&
                    session.IsActive);

            if (!isSessionActive)
            {
                context.Fail("Session is no longer active.");
                return;
            }

            var currentPermissionVersion = await dbContext.Users
                .AsNoTracking()
                .Where(user => user.Id == userId)
                .Select(user => (int?)user.PermissionVersion)
                .FirstOrDefaultAsync();

            if (currentPermissionVersion is null || tokenPermissionVersion != currentPermissionVersion.Value)
            {
                context.Fail("Permission version is outdated.");
            }
        },

        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"Invalid token: {context.Exception.Message}");
            return Task.CompletedTask;
        },

        OnForbidden = context =>
        {
            Console.WriteLine("Access denied");
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseGlobalExceptionHandler();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseCors(FrontendCorsPolicy);

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var environment = services.GetRequiredService<IHostEnvironment>();
    var dbContext = services.GetRequiredService<AppDbContext>();

    if (!environment.IsEnvironment("Testing"))
    {
        await dbContext.Database.MigrateAsync();
    }

    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
    {
        await UserSeed.EnsureRolesAndPermissionsAsync(roleManager);

        if (!await userManager.Users.AnyAsync())
        {
            await UserSeed.SeedAsync(userManager, roleManager);
        }
    }
}

app.Run();