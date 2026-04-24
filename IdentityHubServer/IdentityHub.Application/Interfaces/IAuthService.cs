using IdentityHub.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityHub.Application.Interfaces
{
    public interface IAuthService
    {
        Task RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> RefreshAsync(RefreshTokenRequest request);
        Task LogoutAsync(RefreshTokenRequest request);

        Task ForgotPasswordAsync(ForgotPasswordRequest request);
        Task ResetPasswordAsync(ResetPasswordRequest request);
        Task ChangePasswordAsync(string userId, ChangePasswordRequest request);

        Task ConfirmEmailAsync(string email, string token);
        Task ResendConfirmationAsync(string email);

        Task<ProfileResponse?> UpdateProfileAsync(string userId, UpdateProfileRequest request);
    }
}
