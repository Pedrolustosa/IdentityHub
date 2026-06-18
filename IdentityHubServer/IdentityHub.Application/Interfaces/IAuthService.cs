using IdentityHub.Application.Common.Results;
using IdentityHub.Application.DTOs;

namespace IdentityHub.Application.Interfaces;

public interface IAuthService
{
    Task<Result> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<Result<AuthResponse>> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken);
    Task<Result<MeResponse>> GetMeAsync(string userId, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<UserSessionResponse>>> GetActiveSessionsAsync(string userId, Guid? currentSessionId, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<UserSessionResponse>>> GetRecentSessionsAsync(string userId, Guid? currentSessionId, int take, CancellationToken cancellationToken);
    Task<Result> LogoutAsync(string userId, RefreshTokenRequest request, CancellationToken cancellationToken);
    Task<Result> RevokeSessionAsync(string userId, Guid sessionId, CancellationToken cancellationToken);
    Task<Result> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken);
    Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken);
    Task<Result> ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken cancellationToken);
    Task<Result> ConfirmEmailAsync(string email, string token, CancellationToken cancellationToken);
    Task<Result> ResendConfirmationAsync(string email, CancellationToken cancellationToken);
    Task<Result> UpdateProfileAsync(string userId, UpdateProfileRequest request, CancellationToken cancellationToken);
}