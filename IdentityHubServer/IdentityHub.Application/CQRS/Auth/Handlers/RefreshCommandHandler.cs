using IdentityHub.Application.Common.Errors;
using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.Auth.Commands;
using IdentityHub.Application.DTOs;
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

    public RefreshCommandHandler(
        IAuthRepository repo,
        TokenService tokenService,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _repo = repo;
        _tokenService = tokenService;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<Result<AuthResponse>> Handle(RefreshCommand cmd, CancellationToken ct)
    {
        var token = await _repo.GetRefreshTokenAsync(cmd.Request.RefreshToken, ct);

        if (token is null || token.IsRevoked || token.Expires < DateTime.UtcNow)
            return Result<AuthResponse>.Failure(
                Error.Create("Auth.InvalidRefresh", "Invalid refresh token"));

        var user = token.User;

        var roles = await _userManager.GetRolesAsync(user);

        var access = await _tokenService.GenerateToken(user, roles, _userManager, _roleManager);

        token.IsRevoked = true;

        var newRefresh = _tokenService.GenerateRefreshToken();

        await _repo.AddRefreshTokenAsync(new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = newRefresh,
            UserId = user.Id,
            Created = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddDays(7)
        }, ct);

        await _repo.SaveChangesAsync(ct);

        return Result<AuthResponse>.Success(new AuthResponse
        {
            Token = access,
            RefreshToken = newRefresh
        });
    }
}