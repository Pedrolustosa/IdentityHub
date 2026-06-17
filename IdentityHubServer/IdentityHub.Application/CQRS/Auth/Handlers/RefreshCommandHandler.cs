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

public sealed class RefreshCommandHandler : IRequestHandler<RefreshCommand, Result<AuthResponse>>
{
    private readonly IAuthRepository _repo;
    private readonly TokenService _tokenService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ISecurityAlertService _securityAlertService;

    public RefreshCommandHandler(
        IAuthRepository repo,
        TokenService tokenService,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ISecurityAlertService securityAlertService)
    {
        _repo = repo;
        _tokenService = tokenService;
        _userManager = userManager;
        _roleManager = roleManager;
        _securityAlertService = securityAlertService;
    }

    public async Task<Result<AuthResponse>> Handle(RefreshCommand cmd, CancellationToken ct)
    {
        var refreshTokenHash = _tokenService.ComputeRefreshTokenHash(cmd.Request.RefreshToken);
        var token = await _repo.GetRefreshTokenAsync(refreshTokenHash, ct);

        if (token is null || token.Expires < DateTime.UtcNow)
            return Result<AuthResponse>.Failure(
                Error.Create("Auth.InvalidRefresh", "Invalid refresh token"));

        if (token.IsRevoked)
        {
            await HandleRefreshTokenReuseAsync(token, ct);

            return Result<AuthResponse>.Failure(
                Error.Create("Auth.InvalidRefresh", "Invalid refresh token"));
        }

        var user = token.User;

        if (user is null || user.IsDeleted || !user.IsActive)
        {
            token.IsRevoked = true;
            await _repo.SaveChangesAsync(ct);

            return Result<AuthResponse>.Failure(
                Error.Create("Auth.InvalidRefresh", "Invalid refresh token"));
        }

        var roles = await _userManager.GetRolesAsync(user);

        var access = await _tokenService.GenerateToken(user, token.SessionId, roles, _userManager, _roleManager, ct);

        token.IsRevoked = true;

        var newRefresh = _tokenService.GenerateRefreshToken();
        var newRefreshHash = _tokenService.ComputeRefreshTokenHash(newRefresh);

        await _repo.AddRefreshTokenAsync(new RefreshToken
        {
            Id = Guid.NewGuid(),
            SessionId = token.SessionId,
            TokenHash = newRefreshHash,
            UserId = user.Id,
            Created = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddDays(7)
        }, ct);

        var session = await _repo.GetSessionByIdAsync(token.SessionId, ct);
        if (session is not null && session.IsActive)
            session.LastAccessAt = DateTime.UtcNow;

        await _repo.SaveChangesAsync(ct);

        return Result<AuthResponse>.Success(new AuthResponse
        {
            Token = access,
            RefreshToken = newRefresh
        });
    }

    private async Task HandleRefreshTokenReuseAsync(RefreshToken token, CancellationToken ct)
    {
        var session = await _repo.GetSessionByIdAsync(token.SessionId, ct);
        if (session is not null && session.IsActive)
            await _repo.RevokeSessionAsync(session, ct);

        var activeTokens = await _repo.GetActiveRefreshTokensBySessionAsync(token.SessionId, ct);
        foreach (var activeToken in activeTokens)
            await _repo.RevokeRefreshTokenAsync(activeToken, ct);

        await _repo.SaveChangesAsync(ct);

        if (token.User is not null)
            await _securityAlertService.NotifyRefreshTokenReuseAsync(token.User, token.SessionId, ct);
    }
}