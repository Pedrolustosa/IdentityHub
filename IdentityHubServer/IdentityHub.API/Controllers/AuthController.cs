using IdentityHub.Application.DTOs;
using IdentityHub.Application.Interfaces;
using IdentityHub.Application.Services;
using IdentityHub.Domain.Entities;
using IdentityHub.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IdentityHub.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly TokenService _tokenService;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            TokenService tokenService,
            RoleManager<IdentityRole> roleManager,
            AppDbContext context,
            IEmailService emailService,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _roleManager = roleManager;
            _context = context;
            _emailService = emailService;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Email and password are required");

            var email = NormalizeEmail(request.Email);

            if (await _userManager.FindByEmailAsync(email) != null)
                return BadRequest("User already exists");

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = request.FullName?.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = false
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            if (await _roleManager.RoleExistsAsync("User"))
                await _userManager.AddToRoleAsync(user, "User");

            await SendEmailConfirmation(user);

            await LogSecurityEvent(user.Id, "USER_REGISTERED", "User created");

            return Ok("User created successfully. Please confirm your email.");
        }

        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(string email, string token)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
                return BadRequest("Invalid request");

            var user = await _userManager.FindByEmailAsync(NormalizeEmail(email));

            if (user == null)
                return BadRequest("Invalid token");

            var result = await _userManager.ConfirmEmailAsync(user, Uri.UnescapeDataString(token));

            if (!result.Succeeded)
                return BadRequest("Invalid or expired token");

            await LogSecurityEvent(user.Id, "EMAIL_CONFIRMED", "Email confirmed");

            return Ok("Email confirmed successfully");
        }

        [HttpPost("resend-confirmation")]
        public async Task<IActionResult> ResendConfirmation(ForgotPasswordRequest request)
        {
            var email = NormalizeEmail(request.Email);

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null || user.EmailConfirmed)
                return Ok();

            await SendEmailConfirmation(user);

            return Ok("Confirmation email sent");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Email and password are required");

            var email = NormalizeEmail(request.Email);
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
                return Unauthorized("Invalid credentials");

            if (!user.IsActive)
                return Unauthorized("User is inactive");

            if (!await _userManager.CheckPasswordAsync(user, request.Password))
                return Unauthorized("Invalid credentials");

            if (!user.EmailConfirmed)
                return Unauthorized("Email not confirmed");

            return Ok(await GenerateAuthResponse(user));
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(RefreshTokenRequest request)
        {
            var stored = await _context.RefreshTokens
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Token == request.RefreshToken);

            if (stored == null || stored.IsRevoked || stored.Expires < DateTime.UtcNow)
                return Unauthorized("Invalid refresh token");

            stored.IsRevoked = true;

            await RevokeUserSessions(stored.UserId);

            return Ok(await GenerateAuthResponse(stored.User));
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout(RefreshTokenRequest request)
        {
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(x => x.Token == request.RefreshToken);

            if (token != null)
            {
                token.IsRevoked = true;
                await RevokeUserSessions(token.UserId);
            }

            return Ok("Logged out successfully");
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
        {
            var email = NormalizeEmail(request.Email);
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
                return Ok();

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var link = BuildFrontendLink("reset-password", email, token);

            await _emailService.SendAsync(
                email,
                "Password Reset",
                $"<a href='{link}'>Reset Password</a>"
            );

            await LogSecurityEvent(user.Id, "PASSWORD_RESET_REQUEST", "Requested");

            return Ok();
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
        {
            var user = await _userManager.FindByEmailAsync(NormalizeEmail(request.Email));

            if (user == null)
                return BadRequest("Invalid request");

            var result = await _userManager.ResetPasswordAsync(
                user,
                Uri.UnescapeDataString(request.Token),
                request.NewPassword);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            await RevokeUserSessions(user.Id);

            return Ok("Password reset successfully");
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
        {
            var user = await GetCurrentUser();

            var result = await _userManager.ChangePasswordAsync(
                user,
                request.CurrentPassword,
                request.NewPassword);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            await RevokeUserSessions(user.Id);

            return Ok("Password changed successfully");
        }

        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request)
        {
            var user = await GetCurrentUser();

            if (!string.IsNullOrWhiteSpace(request.FullName))
                user.FullName = request.FullName.Trim();

            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                var email = NormalizeEmail(request.Email);
                var currentEmail = user.Email != null ? NormalizeEmail(user.Email) : string.Empty;

                if (email != currentEmail)
                {
                    var existing = await _userManager.FindByEmailAsync(email);

                    if (existing != null && existing.Id != user.Id)
                        return BadRequest("Email already in use");

                    user.Email = email;
                    user.UserName = email;
                    user.NormalizedEmail = email.ToUpper();
                    user.NormalizedUserName = email.ToUpper();
                    user.EmailConfirmed = false;

                    await SendEmailConfirmation(user);
                }
            }

            await _userManager.UpdateAsync(user);

            return Ok(new { user.Id, user.Email, user.FullName });
        }

        private async Task<AuthResponse> GenerateAuthResponse(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            var accessToken = await _tokenService.GenerateToken(
                user, roles, _userManager, _roleManager);

            var refreshToken = _tokenService.GenerateRefreshToken();

            _context.RefreshTokens.Add(new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = refreshToken,
                UserId = user.Id,
                Created = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddDays(7)
            });

            _context.UserSessions.Add(new UserSession
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            });

            await _context.SaveChangesAsync();

            return new AuthResponse
            {
                Token = accessToken,
                RefreshToken = refreshToken
            };
        }

        private async Task SendEmailConfirmation(ApplicationUser user)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            var link = BuildFrontendLink("confirm-email", user.Email, token);

            await _emailService.SendAsync(
                user.Email,
                "Confirm your account",
                $"<a href='{link}'>Confirm Email</a>"
            );
        }

        private string BuildFrontendLink(string path, string email, string token)
        {
            var baseUrl = (_configuration["Frontend:BaseUrl"] ?? "http://localhost:4200")
                .TrimEnd('/');

            return $"{baseUrl}/{path}?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
        }

        private async Task<ApplicationUser> GetCurrentUser()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return await _userManager.FindByIdAsync(userId);
        }

        private async Task RevokeUserSessions(string userId)
        {
            var sessions = await _context.UserSessions
                .Where(x => x.UserId == userId && x.IsActive)
                .ToListAsync();

            foreach (var s in sessions)
            {
                s.IsActive = false;
                s.RevokedAt = DateTime.UtcNow;
            }

            var tokens = await _context.RefreshTokens
                .Where(x => x.UserId == userId && !x.IsRevoked)
                .ToListAsync();

            foreach (var t in tokens)
                t.IsRevoked = true;

            await _context.SaveChangesAsync();
        }

        private static string NormalizeEmail(string email)
            => email.Trim().ToLower();

        private async Task LogSecurityEvent(string? userId, string type, string description)
        {
            _context.SecurityEvents.Add(new SecurityEvent
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = type,
                Description = description,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }
    }
}