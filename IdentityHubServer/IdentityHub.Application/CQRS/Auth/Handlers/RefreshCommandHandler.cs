using IdentityHub.Application.CQRS.Auth.Commands;
using IdentityHub.Application.DTOs;
using IdentityHub.Application.Services;
using IdentityHub.Domain.Entities;
using IdentityHub.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace IdentityHub.Application.CQRS.Auth.Handlers;

public sealed class RefreshCommandHandler : IRequestHandler<RefreshCommand, AuthResponse>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly TokenService _tokenService;
    private readonly IAuthRepository _authRepository;

    public RefreshCommandHandler(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        TokenService tokenService,
        IAuthRepository authRepository)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _tokenService = tokenService;
        _authRepository = authRepository;
    }

    public async Task<AuthResponse> Handle(
        RefreshCommand command,
        CancellationToken cancellationToken)
    {
        var storedToken = await _authRepository.GetRefreshTokenAsync(
            command.Request.RefreshToken,
            cancellationToken);

        if (storedToken == null ||
            storedToken.IsRevoked ||
            storedToken.Expires < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Invalid refresh token");

        storedToken.IsRevoked = true;

        var user = storedToken.User;
        var roles = await _userManager.GetRolesAsync(user);

        var newAccessToken = await _tokenService.GenerateToken(
            user,
            roles,
            _userManager,
            _roleManager);

        var newRefreshToken = _tokenService.GenerateRefreshToken();

        await _authRepository.AddRefreshTokenAsync(new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = newRefreshToken,
            UserId = user.Id,
            Created = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        }, cancellationToken);

        await _authRepository.SaveChangesAsync(cancellationToken);

        return new AuthResponse
        {
            Token = newAccessToken,
            RefreshToken = newRefreshToken
        };
    }
}