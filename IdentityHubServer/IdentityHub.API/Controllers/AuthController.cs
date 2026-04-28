using IdentityHub.Application.DTOs;
using IdentityHub.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IdentityHub.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _service;

        public AuthController(IAuthService service)
        {
            _service = service;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
        {
            await _service.RegisterAsync(request, cancellationToken);
            return Ok();
        }

        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(string email, string token, CancellationToken cancellationToken)
        {
            await _service.ConfirmEmailAsync(email, token, cancellationToken);
            return Ok();
        }

        [HttpPost("resend-confirmation")]
        public async Task<IActionResult> ResendConfirmation(ForgotPasswordRequest request, CancellationToken cancellationToken)
        {
            await _service.ResendConfirmationAsync(request.Email, cancellationToken);
            return Ok();
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
            => Ok(await _service.LoginAsync(request, cancellationToken));

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken)
            => Ok(await _service.RefreshAsync(request, cancellationToken));

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout(RefreshTokenRequest request, CancellationToken cancellationToken)
        {
            await _service.LogoutAsync(request, cancellationToken);
            return Ok();
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
        {
            await _service.ForgotPasswordAsync(request, cancellationToken);
            return Ok();
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
        {
            await _service.ResetPasswordAsync(request, cancellationToken);
            return Ok();
        }

        [Authorize(Policy = "Users.ChangePassword")]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            await _service.ChangePasswordAsync(userId, request, cancellationToken);
            return Ok();
        }

        /// <summary>Updates the signed-in user's own profile (e.g. display name). Email is not changed via this API.</summary>
        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request, CancellationToken cancellationToken)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            try
            {
                var profile = await _service.UpdateProfileAsync(userId, request, cancellationToken);
                if (profile == null)
                {
                    return NotFound();
                }

                return Ok(profile);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
