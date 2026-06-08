using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.Auth.Commands;
using IdentityHub.Application.CQRS.Auth.Queries;
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

    public Task<Result> RegisterAsync(RegisterRequest r, CancellationToken ct)
        => _sender.Send(new RegisterCommand(r), ct);

    public Task<Result<AuthResponse>> LoginAsync(LoginRequest r, CancellationToken ct)
        => _sender.Send(new LoginCommand(r), ct);

    public Task<Result<AuthResponse>> RefreshAsync(RefreshTokenRequest r, CancellationToken ct)
        => _sender.Send(new RefreshCommand(r), ct);

    public Task<Result<MeResponse>> GetMeAsync(string u, CancellationToken ct)
        => _sender.Send(new GetMeQuery(u), ct);

    public Task<Result<IReadOnlyList<UserSessionResponse>>> GetActiveSessionsAsync(string u, Guid? s, CancellationToken ct)
        => _sender.Send(new GetActiveSessionsQuery(u, s), ct);

    public Task<Result> LogoutAsync(string u, RefreshTokenRequest r, CancellationToken ct)
        => _sender.Send(new LogoutCommand(u, r), ct);

    public Task<Result> RevokeSessionAsync(string u, Guid s, CancellationToken ct)
        => _sender.Send(new RevokeSessionCommand(u, s), ct);

    public Task<Result> ForgotPasswordAsync(ForgotPasswordRequest r, CancellationToken ct)
        => _sender.Send(new ForgotPasswordCommand(r), ct);

    public Task<Result> ResetPasswordAsync(ResetPasswordRequest r, CancellationToken ct)
        => _sender.Send(new ResetPasswordCommand(r), ct);

    public Task<Result> ChangePasswordAsync(string u, ChangePasswordRequest r, CancellationToken ct)
        => _sender.Send(new ChangePasswordCommand(u, r), ct);

    public Task<Result> ConfirmEmailAsync(string e, string t, CancellationToken ct)
        => _sender.Send(new ConfirmEmailCommand(e, t), ct);

    public Task<Result> ResendConfirmationAsync(string e, CancellationToken ct)
        => _sender.Send(new ResendConfirmationCommand(e), ct);

    public Task<Result> UpdateProfileAsync(string u, UpdateProfileRequest r, CancellationToken ct)
        => _sender.Send(new UpdateProfileCommand(u, r), ct);
}