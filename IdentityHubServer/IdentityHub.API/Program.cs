using IdentityHub.API.Authorization;
using IdentityHub.IoC;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 🔐 Swagger com suporte a JWT
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using Bearer scheme. Example: \"Bearer eyJhbGciOiJIUzI1NiIs...\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement()
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

// 🔌 Infraestrutura (EF + Identity)
builder.Services.AddInfrastructure(builder.Configuration);

// 🔐 Authorization Handler (permissions)
builder.Services.AddSingleton<IAuthorizationHandler, PermissionHandler>();

// 🔐 Authorization (dinâmico por policy)
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Users.View", policy =>
    policy.Requirements.Add(new PermissionRequirement("Users.View")));
});

// 🔐 JWT Config
var jwtSettings = builder.Configuration.GetSection("Jwt");

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
            Encoding.UTF8.GetBytes(jwtSettings["Key"]))
    };

    // 🔍 Logs úteis (debug de 401 / 403)
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"❌ Token inválido: {context.Exception.Message}");
            return Task.CompletedTask;
        },
        OnForbidden = context =>
        {
            Console.WriteLine("🚫 Acesso negado (403)");
            return Task.CompletedTask;
        }
    };
});

var app = builder.Build();

// 📘 Swagger
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

// 🔐 Ordem correta (CRÍTICO)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 🔹 Seed de Roles
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    string[] roles = { "Admin", "Manager", "User" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}

app.Run();