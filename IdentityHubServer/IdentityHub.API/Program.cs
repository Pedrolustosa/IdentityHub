using IdentityHub.API.Authorization;
using IdentityHub.API.Middlewares;
using IdentityHub.Application.DTOs;
using IdentityHub.Domain.Interfaces;
using IdentityHub.Domain.Entities;
using IdentityHub.Infrastructure.Data.Seed;
using IdentityHub.Infrastructure.Repositories;
using IdentityHub.Infrastructure.Security;
using IdentityHub.IoC;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

const string FrontendCorsPolicy = "FrontendPolicy";

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using Bearer scheme.",
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

builder.Services.AddAuthorization();

builder.Services.AddSingleton<IAuthorizationHandler, PermissionHandler>();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
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

var app = builder.Build();

app.UseGlobalExceptionHandler();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors(FrontendCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    if (!await userManager.Users.AnyAsync())
    {
        await UserSeed.SeedAsync(userManager, roleManager);
    }
}

app.Run();

public partial class Program
{
}