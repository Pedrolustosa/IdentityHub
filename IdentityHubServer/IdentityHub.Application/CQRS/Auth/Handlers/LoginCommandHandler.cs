using IdentityHub.Application.Common.Errors;
using IdentityHub.Application.Common.Results;
using IdentityHub.Application.Common.Security;
using IdentityHub.Application.CQRS.Auth.Commands;
using IdentityHub.Application.DTOs;
using IdentityHub.Application.Interfaces;
using IdentityHub.Application.Services;
using IdentityHub.Domain.Entities;
using IdentityHub.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace IdentityHub.Application.CQRS.Auth.Handlers;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly TokenService _tokenService;
    private readonly IAuthRepository _authRepository;
    private readonly IClientDeviceInfoProvider _clientDeviceInfoProvider;
    private readonly ISecurityAlertService _securityAlertService;
    private readonly ISecuritySettingsService _securitySettingsService;

    public LoginCommandHandler(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        TokenService tokenService,
        IAuthRepository authRepository,
        IClientDeviceInfoProvider clientDeviceInfoProvider,
        ISecurityAlertService securityAlertService,
        ISecuritySettingsService securitySettingsService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _tokenService = tokenService;
        _authRepository = authRepository;
        _clientDeviceInfoProvider = clientDeviceInfoProvider;
        _securityAlertService = securityAlertService;
        _securitySettingsService = securitySettingsService;
    }

    public async Task<Result<AuthResponse>> Handle(
        LoginCommand command,
        CancellationToken cancellationToken)
    {
        var email = command.Request.Email.Trim().ToLowerInvariant();
        var settings = await _securitySettingsService.GetOrDefaultAsync(cancellationToken);

        var user = await _userManager.FindByEmailAsync(email);

        if (user is null || !user.IsActive || user.IsDeleted)
            return Result<AuthResponse>.Failure(
                Error.Create("Auth.InvalidCredentials", "Invalid credentials"));

        if (await _userManager.IsLockedOutAsync(user))
            return Result<AuthResponse>.Failure(
                Error.Create("Auth.AccountLocked", "Account is temporarily locked"));

        var passwordValid = await _userManager.CheckPasswordAsync(
            user,
            command.Request.Password);

        if (!passwordValid)
        {
            await _userManager.AccessFailedAsync(user);

            var failedCount = await _userManager.GetAccessFailedCountAsync(user);
            var maxAttempts = settings.MaxLoginAttempts > 0
                ? settings.MaxLoginAttempts
                : SecuritySettingsDefaults.MaxLoginAttempts;

            if (failedCount >= maxAttempts)
            {
                var lockMinutes = settings.LockDurationMinutes > 0
                    ? settings.LockDurationMinutes
                    : SecuritySettingsDefaults.LockDurationMinutes;

                await _userManager.SetLockoutEndDateAsync(
                    user,
                    DateTimeOffset.UtcNow.AddMinutes(lockMinutes));
            }

            await _securityAlertService.NotifySuspiciousLoginAsync(user, "Invalid credentials", cancellationToken);
            return Result<AuthResponse>.Failure(
                Error.Create("Auth.InvalidCredentials", "Invalid credentials"));
        }

        if (settings.RequireEmailConfirmation && !user.EmailConfirmed)
            return Result<AuthResponse>.Failure(
                Error.Create("Auth.EmailNotConfirmed", "Email not confirmed"));

        await _userManager.ResetAccessFailedCountAsync(user);

        var roles = await _userManager.GetRolesAsync(user);

        var sessionId = Guid.NewGuid();

        var accessToken = await _tokenService.GenerateToken(
            user,
            sessionId,
            roles,
            _userManager,
            _roleManager,
            cancellationToken);

        var refreshToken = _tokenService.GenerateRefreshToken();
        var refreshTokenHash = _tokenService.ComputeRefreshTokenHash(refreshToken);
        var (ipAddress, browser, operatingSystem) = _clientDeviceInfoProvider.GetCurrent();

        var refreshTokenDays = settings.RefreshTokenDays > 0
            ? settings.RefreshTokenDays
            : SecuritySettingsDefaults.RefreshTokenDays;

        await _authRepository.AddRefreshTokenAsync(new RefreshToken
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            TokenHash = refreshTokenHash,
            UserId = user.Id,
            Created = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddDays(refreshTokenDays),
            IsRevoked = false
        }, cancellationToken);

        await _authRepository.AddSessionAsync(new UserSession
        {
            Id = sessionId,
            UserId = user.Id,
            IpAddress = ipAddress,
            Browser = browser,
            OperatingSystem = operatingSystem,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        }, cancellationToken);

        await _authRepository.SaveChangesAsync(cancellationToken);

        return Result<AuthResponse>.Success(new AuthResponse
        {
            Token = accessToken,
            RefreshToken = refreshToken,
            RefreshTokenDays = refreshTokenDays
        });
    }
}
