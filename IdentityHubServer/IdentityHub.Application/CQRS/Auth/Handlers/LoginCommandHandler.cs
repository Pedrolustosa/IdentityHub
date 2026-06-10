using IdentityHub.Application.Common.Errors;
using IdentityHub.Application.Common.Results;
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

    public LoginCommandHandler(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        TokenService tokenService,
        IAuthRepository authRepository,
        IClientDeviceInfoProvider clientDeviceInfoProvider,
        ISecurityAlertService securityAlertService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _tokenService = tokenService;
        _authRepository = authRepository;
        _clientDeviceInfoProvider = clientDeviceInfoProvider;
        _securityAlertService = securityAlertService;
    }

    public async Task<Result<AuthResponse>> Handle(
        LoginCommand command,
        CancellationToken cancellationToken)
    {
        var email = command.Request.Email.Trim().ToLowerInvariant();

        var user = await _userManager.FindByEmailAsync(email);

        if (user is null || !user.IsActive || user.IsDeleted)
            return Result<AuthResponse>.Failure(
                Error.Create("Auth.InvalidCredentials", "Invalid credentials"));

        var passwordValid = await _userManager.CheckPasswordAsync(
            user,
            command.Request.Password);

        if (!passwordValid)
        {
            await _securityAlertService.NotifySuspiciousLoginAsync(user, "Invalid credentials", cancellationToken);
            return Result<AuthResponse>.Failure(
                Error.Create("Auth.InvalidCredentials", "Invalid credentials"));
        }

        if (!user.EmailConfirmed)
            return Result<AuthResponse>.Failure(
                Error.Create("Auth.EmailNotConfirmed", "Email not confirmed"));

        var roles = await _userManager.GetRolesAsync(user);

        var sessionId = Guid.NewGuid();

        var accessToken = await _tokenService.GenerateToken(
            user,
            sessionId,
            roles,
            _userManager,
            _roleManager);

        var refreshToken = _tokenService.GenerateRefreshToken();
        var refreshTokenHash = _tokenService.ComputeRefreshTokenHash(refreshToken);
        var (ipAddress, browser, operatingSystem) = _clientDeviceInfoProvider.GetCurrent();

        await _authRepository.AddRefreshTokenAsync(new RefreshToken
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            TokenHash = refreshTokenHash,
            UserId = user.Id,
            Created = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddDays(7),
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
            RefreshToken = refreshToken
        });
    }
}