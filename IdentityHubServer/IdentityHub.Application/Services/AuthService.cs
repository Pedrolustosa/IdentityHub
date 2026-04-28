using System.Linq;
using IdentityHub.Application.DTOs;
using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Entities;
using IdentityHub.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace IdentityHub.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly TokenService _tokenService;
        private readonly IEmailService _emailService;
        private readonly IAuthRepository _authRepository;
        private readonly IConfiguration _configuration;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            TokenService tokenService,
            IEmailService emailService,
            IAuthRepository authRepository,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _tokenService = tokenService;
            _emailService = emailService;
            _authRepository = authRepository;
            _configuration = configuration;
        }

        public async Task RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var email = request.Email.Trim().ToLower();

            var exists = await _userManager.FindByEmailAsync(email);
            if (exists != null)
                throw new Exception("User already exists");

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = request.FullName?.Trim(),
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                EmailConfirmed = false
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                throw new Exception(string.Join(",", result.Errors.Select(e => e.Description)));

            cancellationToken.ThrowIfCancellationRequested();
            if (await _roleManager.RoleExistsAsync("User"))
                await _userManager.AddToRoleAsync(user, "User");

            await SendConfirmationEmail(user, cancellationToken);
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var email = request.Email.Trim().ToLower();

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || !user.IsActive)
                throw new Exception("Invalid credentials");

            if (!await _userManager.CheckPasswordAsync(user, request.Password))
                throw new Exception("Invalid credentials");

            if (!user.EmailConfirmed)
                throw new Exception("Email not confirmed");

            var roles = await _userManager.GetRolesAsync(user);
            var accessToken = await _tokenService.GenerateToken(user, roles, _userManager, _roleManager, cancellationToken);
            var refreshToken = _tokenService.GenerateRefreshToken();

            await _authRepository.SaveRefreshTokenAsync(new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = refreshToken,
                UserId = user.Id,
                Created = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            }, cancellationToken);

            await _authRepository.CreateSessionAsync(new UserSession
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            }, cancellationToken);

            await _authRepository.SaveChangesAsync(cancellationToken);

            return new AuthResponse
            {
                Token = accessToken,
                RefreshToken = refreshToken
            };
        }

        public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var token = await _authRepository.GetRefreshTokenAsync(request.RefreshToken, cancellationToken);
            if (token == null || token.IsRevoked || token.Expires < DateTime.UtcNow)
                throw new Exception("Invalid refresh token");

            var user = token.User;
            var roles = await _userManager.GetRolesAsync(user);
            var newAccessToken = await _tokenService.GenerateToken(user, roles, _userManager, _roleManager, cancellationToken);

            await _authRepository.RevokeRefreshTokenAsync(token, cancellationToken);

            var newRefreshToken = _tokenService.GenerateRefreshToken();
            await _authRepository.SaveRefreshTokenAsync(new RefreshToken
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

        public async Task LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
        {
            var token = await _authRepository.GetRefreshTokenAsync(request.RefreshToken, cancellationToken);
            if (token != null)
            {
                await _authRepository.RevokeRefreshTokenAsync(token, cancellationToken);
                await _authRepository.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var email = request.Email.Trim().ToLower();
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return;

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encoded = Uri.EscapeDataString(token);
            var baseUrl = (_configuration["Frontend:BaseUrl"] ?? "").TrimEnd('/');
            var link = $"{baseUrl}/reset-password?email={email}&token={encoded}";

            await _emailService.SendAsync(
                email,
                "Reset Password",
                $"Click here: <a href='{link}'>Reset</a>",
                cancellationToken);
        }

        public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var user = await _userManager.FindByEmailAsync(request.Email);
            var token = Uri.UnescapeDataString(request.Token);
            var result = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);

            if (!result.Succeeded)
                throw new Exception("Reset failed");

            await RevokeAllSessions(user.Id, cancellationToken);
        }

        public async Task ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var user = await _userManager.FindByIdAsync(userId);
            var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

            if (!result.Succeeded)
                throw new Exception("Change failed");

            await RevokeAllSessions(user.Id, cancellationToken);
        }

        public async Task ConfirmEmailAsync(string email, string token, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var user = await _userManager.FindByEmailAsync(email);
            var decoded = Uri.UnescapeDataString(token);
            var result = await _userManager.ConfirmEmailAsync(user, decoded);

            if (!result.Succeeded)
                throw new Exception("Invalid token");
        }

        public async Task ResendConfirmationAsync(string email, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || user.EmailConfirmed)
                return;

            await SendConfirmationEmail(user, cancellationToken);
        }

        public async Task<ProfileResponse?> UpdateProfileAsync(string userId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return null;

            if (!string.IsNullOrWhiteSpace(request.FullName))
                user.FullName = request.FullName.Trim();

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var msg = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException(msg);
            }

            return new ProfileResponse
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName ?? string.Empty
            };
        }

        private async Task SendConfirmationEmail(ApplicationUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encoded = Uri.EscapeDataString(token);
            var baseUrl = (_configuration["Frontend:BaseUrl"] ?? "").TrimEnd('/');
            var link = $"{baseUrl}/confirm-email?email={user.Email}&token={encoded}";

            await _emailService.SendAsync(
                user.Email,
                "Confirm Email",
                $"Click here: <a href='{link}'>Confirm</a>",
                cancellationToken);
        }

        private async Task RevokeAllSessions(string userId, CancellationToken cancellationToken)
        {
            var sessions = await _authRepository.GetActiveSessionsAsync(userId, cancellationToken);
            foreach (var session in sessions)
            {
                cancellationToken.ThrowIfCancellationRequested();
                session.IsActive = false;
                session.RevokedAt = DateTime.UtcNow;
            }

            var tokens = await _authRepository.GetActiveRefreshTokensAsync(userId, cancellationToken);
            foreach (var token in tokens)
            {
                cancellationToken.ThrowIfCancellationRequested();
                token.IsRevoked = true;
            }

            await _authRepository.SaveChangesAsync(cancellationToken);
        }
    }
}
