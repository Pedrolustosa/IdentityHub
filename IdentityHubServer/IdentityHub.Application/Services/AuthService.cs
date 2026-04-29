using IdentityHub.Application.CQRS.Auth.Commands;
using IdentityHub.Application.DTOs;
using IdentityHub.Application.Interfaces;
using MediatR;

namespace IdentityHub.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly ISender _sender;

    public AuthService(ISender sender)
    {
        _sender = sender;
    }

    public async Task RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new RegisterCommand(request), cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        return await _sender.Send(new LoginCommand(request), cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        return await _sender.Send(new RefreshCommand(request), cancellationToken);
    }

    public async Task LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new LogoutCommand(request), cancellationToken);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new ForgotPasswordCommand(request), cancellationToken);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new ResetPasswordCommand(request), cancellationToken);
    }

    public async Task ChangePasswordAsync(
        string userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new ChangePasswordCommand(userId, request), cancellationToken);
    }

    public async Task ConfirmEmailAsync(
        string email,
        string token,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new ConfirmEmailCommand(email, token), cancellationToken);
    }

    public async Task ResendConfirmationAsync(string email, CancellationToken cancellationToken)
    {
        await _sender.Send(new ResendConfirmationCommand(email), cancellationToken);
    }

    public async Task UpdateProfileAsync(
        string userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new UpdateProfileCommand(userId, request), cancellationToken);
    }
}