using IdentityHub.Application.DTOs;

namespace IdentityHub.Application.Interfaces
{
    public interface IAuthService
    {
        Task RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
        Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
        Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
        Task LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);

        Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
        Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
        Task ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);

        Task ConfirmEmailAsync(string email, string token, CancellationToken cancellationToken = default);
        Task ResendConfirmationAsync(string email, CancellationToken cancellationToken = default);

        Task<ProfileResponse?> UpdateProfileAsync(string userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);
    }
}
