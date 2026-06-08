using IdentityHub.API.Extensions;
using IdentityHub.Application.DTOs;
using IdentityHub.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IdentityHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _service;

    public AuthController(IAuthService service)
    {
        _service = service;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.RegisterAsync(request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(
        string email,
        string token,
        CancellationToken cancellationToken)
    {
        var result = await _service.ConfirmEmailAsync(email, token, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("resend-confirmation")]
    public async Task<IActionResult> ResendConfirmation(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.ResendConfirmationAsync(request.Email, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.LoginAsync(request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.RefreshAsync(request, cancellationToken);
        return result.ToActionResult();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var result = await _service.GetMeAsync(userId, cancellationToken);
        return result.ToActionResult();
    }

    [Authorize]
    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        Guid? currentSessionId = null;
        var sidValue = User.FindFirst("sid")?.Value;
        if (Guid.TryParse(sidValue, out var parsedSessionId))
            currentSessionId = parsedSessionId;

        var result = await _service.GetActiveSessionsAsync(userId, currentSessionId, cancellationToken);
        return result.ToActionResult();
    }

    [Authorize]
    [HttpDelete("sessions/{sessionId:guid}")]
    public async Task<IActionResult> RevokeSession(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var result = await _service.RevokeSessionAsync(userId, sessionId, cancellationToken);
        return result.ToActionResult();
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var result = await _service.LogoutAsync(userId, request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.ForgotPasswordAsync(request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.ResetPasswordAsync(request, cancellationToken);
        return result.ToActionResult();
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var result = await _service.ChangePasswordAsync(userId, request, cancellationToken);
        return result.ToActionResult();
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(
        UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var result = await _service.UpdateProfileAsync(userId, request, cancellationToken);
        return result.ToActionResult();
    }
}