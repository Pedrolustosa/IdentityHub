using IdentityHub.Application.Common.Results;
using IdentityHub.Application.DTOs;

namespace IdentityHub.Application.Interfaces;

public interface IAuthService
{
    Task<Result> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<Result<AuthResponse>> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken);
    Task<Result> LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken);
    Task<Result> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken);
    Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken);
    Task<Result> ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken cancellationToken);
    Task<Result> ConfirmEmailAsync(string email, string token, CancellationToken cancellationToken);
    Task<Result> ResendConfirmationAsync(string email, CancellationToken cancellationToken);
    Task<Result> UpdateProfileAsync(string userId, UpdateProfileRequest request, CancellationToken cancellationToken);
}