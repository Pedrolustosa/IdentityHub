using FluentValidation;
using IdentityHub.Application.Common.Behaviors;
using IdentityHub.Application.Interfaces;
using IdentityHub.Application.Services;
using IdentityHub.Domain.Entities;
using IdentityHub.Domain.Interfaces;
using IdentityHub.Infrastructure.Data;
using IdentityHub.Infrastructure.Repositories;
using IdentityHub.Infrastructure.Security;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityHub.IoC
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            services.AddValidatorsFromAssembly(typeof(AuthService).Assembly);

            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(AuthService).Assembly);
                cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });
            services.AddHttpContextAccessor();

            services.AddScoped<TokenService>();

            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IRoleClaimService, RoleClaimService>();
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<IAuditLogService, AuditLogService>();
            services.AddScoped<ISecurityAlertsService, SecurityAlertsService>();

            services.AddScoped<IAuthRepository, AuthRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IDashboardRepository, DashboardRepository>();
            services.AddScoped<IAuditLogRepository, AuditLogRepository>();
            services.AddScoped<ISecurityAlertRepository, SecurityAlertRepository>();

            services.AddScoped<ICurrentUserContext, CurrentUserContext>();
            services.AddScoped<IClientDeviceInfoProvider, ClientDeviceInfoProvider>();

            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IEmailTemplateBuilder, EmailTemplateBuilder>();
            services.AddScoped<ISecurityAlertService, SecurityAlertService>();

            return services;
        }
    }
}