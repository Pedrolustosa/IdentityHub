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

        public async Task RegisterAsync(RegisterRequest request)
        {
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

            if (await _roleManager.RoleExistsAsync("User"))
                await _userManager.AddToRoleAsync(user, "User");

            await SendConfirmationEmail(user);
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var email = request.Email.Trim().ToLower();

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null || !user.IsActive)
                throw new Exception("Invalid credentials");

            if (!await _userManager.CheckPasswordAsync(user, request.Password))
                throw new Exception("Invalid credentials");

            if (!user.EmailConfirmed)
                throw new Exception("Email not confirmed");

            var roles = await _userManager.GetRolesAsync(user);

            var accessToken = await _tokenService.GenerateToken(
                user, roles, _userManager, _roleManager);

            var refreshToken = _tokenService.GenerateRefreshToken();

            await _authRepository.SaveRefreshTokenAsync(new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = refreshToken,
                UserId = user.Id,
                Created = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            });

            await _authRepository.CreateSessionAsync(new UserSession
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            });

            await _authRepository.SaveChangesAsync();

            return new AuthResponse
            {
                Token = accessToken,
                RefreshToken = refreshToken
            };
        }

        public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request)
        {
            var token = await _authRepository.GetRefreshTokenAsync(request.RefreshToken);

            if (token == null || token.IsRevoked || token.Expires < DateTime.UtcNow)
                throw new Exception("Invalid refresh token");

            var user = token.User;

            var roles = await _userManager.GetRolesAsync(user);

            var newAccessToken = await _tokenService.GenerateToken(
                user, roles, _userManager, _roleManager);

            await _authRepository.RevokeRefreshTokenAsync(token);

            var newRefreshToken = _tokenService.GenerateRefreshToken();

            await _authRepository.SaveRefreshTokenAsync(new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = newRefreshToken,
                UserId = user.Id,
                Created = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            });

            await _authRepository.SaveChangesAsync();

            return new AuthResponse
            {
                Token = newAccessToken,
                RefreshToken = newRefreshToken
            };
        }

        public async Task LogoutAsync(RefreshTokenRequest request)
        {
            var token = await _authRepository.GetRefreshTokenAsync(request.RefreshToken);

            if (token != null)
            {
                await _authRepository.RevokeRefreshTokenAsync(token);
                await _authRepository.SaveChangesAsync();
            }
        }

        public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var email = request.Email.Trim().ToLower();

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return;

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encoded = Uri.EscapeDataString(token);

            var baseUrl = (_configuration["Frontend:BaseUrl"] ?? "").TrimEnd('/');

            var link = $"{baseUrl}/reset-password?email={email}&token={encoded}";

            await _emailService.SendAsync(
                email,
                "Reset Password",
                $"Click here: <a href='{link}'>Reset</a>");
        }

        public async Task ResetPasswordAsync(ResetPasswordRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            var token = Uri.UnescapeDataString(request.Token);

            var result = await _userManager.ResetPasswordAsync(
                user, token, request.NewPassword);

            if (!result.Succeeded)
                throw new Exception("Reset failed");

            await RevokeAllSessions(user.Id);
        }

        public async Task ChangePasswordAsync(string userId, ChangePasswordRequest request)
        {
            var user = await _userManager.FindByIdAsync(userId);

            var result = await _userManager.ChangePasswordAsync(
                user, request.CurrentPassword, request.NewPassword);

            if (!result.Succeeded)
                throw new Exception("Change failed");

            await RevokeAllSessions(user.Id);
        }

        public async Task ConfirmEmailAsync(string email, string token)
        {
            var user = await _userManager.FindByEmailAsync(email);

            var decoded = Uri.UnescapeDataString(token);

            var result = await _userManager.ConfirmEmailAsync(user, decoded);

            if (!result.Succeeded)
                throw new Exception("Invalid token");
        }

        public async Task ResendConfirmationAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null || user.EmailConfirmed) return;

            await SendConfirmationEmail(user);
        }

        public async Task UpdateProfileAsync(string userId, UpdateProfileRequest request)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (!string.IsNullOrWhiteSpace(request.FullName))
                user.FullName = request.FullName.Trim();

            await _userManager.UpdateAsync(user);
        }

        private async Task SendConfirmationEmail(ApplicationUser user)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encoded = Uri.EscapeDataString(token);

            var baseUrl = (_configuration["Frontend:BaseUrl"] ?? "").TrimEnd('/');

            var link = $"{baseUrl}/confirm-email?email={user.Email}&token={encoded}";

            await _emailService.SendAsync(
                user.Email,
                "Confirm Email",
                $"Click here: <a href='{link}'>Confirm</a>");
        }

        private async Task RevokeAllSessions(string userId)
        {
            var sessions = await _authRepository.GetActiveSessionsAsync(userId);

            foreach (var session in sessions)
            {
                session.IsActive = false;
                session.RevokedAt = DateTime.UtcNow;
            }

            var tokens = await _authRepository.GetActiveRefreshTokensAsync(userId);

            foreach (var token in tokens)
            {
                token.IsRevoked = true;
            }

            await _authRepository.SaveChangesAsync();
        }
    }
}