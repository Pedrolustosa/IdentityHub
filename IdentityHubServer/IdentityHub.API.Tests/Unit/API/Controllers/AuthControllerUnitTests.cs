using IdentityHub.API.Controllers;
using IdentityHub.Application.Common.Errors;
using IdentityHub.Application.Common.Results;
using IdentityHub.Application.DTOs;
using IdentityHub.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Xunit;

namespace IdentityHub.API.Tests;

public sealed class AuthControllerUnitTests
{
    [Fact]
    public async Task GetMe_ShouldReturnUnauthorized_WhenUserIdClaimIsMissing()
    {
        var controller = CreateController(new FakeAuthService());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            }
        };

        var action = await controller.GetMe(CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(action);
    }

    [Fact]
    public async Task GetSessions_ShouldPassParsedSidToService_AndReturnOk()
    {
        var sid = Guid.NewGuid();
        var service = new FakeAuthService
        {
            GetActiveSessionsResult = Result<IReadOnlyList<UserSessionResponse>>.Success(
            [
                new UserSessionResponse { Id = Guid.NewGuid(), IsCurrent = true },
                new UserSessionResponse { Id = Guid.NewGuid(), IsCurrent = false }
            ])
        };

        var controller = CreateController(service, userId: "user-1", sid: sid.ToString());

        var action = await controller.GetSessions(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action);
        Assert.NotNull(ok.Value);
        Assert.Equal("user-1", service.LastUserIdForGetSessions);
        Assert.Equal(sid, service.LastCurrentSessionIdForGetSessions);
    }

    [Fact]
    public async Task Refresh_ShouldUseCookieToken_WhenRequestTokenIsMissing()
    {
        var service = new FakeAuthService
        {
            RefreshResult = Result<AuthResponse>.Success(new AuthResponse
            {
                Token = "access-token",
                RefreshToken = "new-refresh"
            })
        };

        var controller = CreateController(service);
        controller.ControllerContext.HttpContext.Request.Headers.Cookie = "ih_refresh=cookie-token";

        var action = await controller.Refresh(new RefreshTokenRequest { RefreshToken = "  " }, CancellationToken.None);

        Assert.IsType<OkObjectResult>(action);
        Assert.Equal("cookie-token", service.LastRefreshRequest.RefreshToken);

        var setCookie = controller.ControllerContext.HttpContext.Response.Headers.SetCookie.ToString();
        Assert.Contains("ih_refresh=new-refresh", setCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Refresh_ShouldPreferRequestToken_WhenProvided()
    {
        var service = new FakeAuthService
        {
            RefreshResult = Result<AuthResponse>.Success(new AuthResponse
            {
                Token = "access-token",
                RefreshToken = "new-refresh"
            })
        };

        var controller = CreateController(service);
        controller.ControllerContext.HttpContext.Request.Headers.Cookie = "ih_refresh=cookie-token";

        var action = await controller.Refresh(new RefreshTokenRequest { RefreshToken = " request-token " }, CancellationToken.None);

        Assert.IsType<OkObjectResult>(action);
        Assert.Equal("request-token", service.LastRefreshRequest.RefreshToken);
    }

    [Fact]
    public async Task RevokeOtherSessions_ShouldRevokeOnlyNonCurrentSessions_AndReturnNoContent()
    {
        var current = new UserSessionResponse { Id = Guid.NewGuid(), IsCurrent = true };
        var other1 = new UserSessionResponse { Id = Guid.NewGuid(), IsCurrent = false };
        var other2 = new UserSessionResponse { Id = Guid.NewGuid(), IsCurrent = false };

        var service = new FakeAuthService
        {
            GetActiveSessionsResult = Result<IReadOnlyList<UserSessionResponse>>.Success([current, other1, other2]),
            RevokeSessionResult = Result.Success()
        };

        var controller = CreateController(service, userId: "user-2", sid: current.Id.ToString());

        var action = await controller.RevokeOtherSessions(CancellationToken.None);

        Assert.IsType<NoContentResult>(action);
        Assert.Equal(2, service.RevokedSessionIds.Count);
        Assert.Contains(other1.Id, service.RevokedSessionIds);
        Assert.Contains(other2.Id, service.RevokedSessionIds);
        Assert.DoesNotContain(current.Id, service.RevokedSessionIds);
    }

    [Fact]
    public async Task Logout_ShouldClearCookie_WhenServiceSucceeds()
    {
        var service = new FakeAuthService { LogoutResult = Result.Success() };
        var controller = CreateController(service, userId: "user-logout");

        var action = await controller.Logout(new RefreshTokenRequest { RefreshToken = "abc" }, CancellationToken.None);

        Assert.IsType<OkResult>(action);

        var setCookie = controller.ControllerContext.HttpContext.Response.Headers.SetCookie.ToString();
        Assert.Contains("ih_refresh=", setCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RevokeUserSessions_ShouldReturnFirstRevokeFailure()
    {
        var targetUserId = "target-user";
        var session1 = new UserSessionResponse { Id = Guid.NewGuid(), IsCurrent = false };
        var session2 = new UserSessionResponse { Id = Guid.NewGuid(), IsCurrent = false };

        var service = new FakeAuthService
        {
            GetActiveSessionsResult = Result<IReadOnlyList<UserSessionResponse>>.Success([session1, session2]),
            RevokeSessionById = id => id == session2.Id
                ? Result.Failure(Error.Create("Auth.Forbidden", "blocked"))
                : Result.Success()
        };

        var controller = CreateController(service, userId: "admin");

        var action = await controller.RevokeUserSessions(targetUserId, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(action);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    private static AuthController CreateController(FakeAuthService service, string? userId = null, string? sid = null)
    {
        var controller = new AuthController(service);

        var identity = new ClaimsIdentity();
        if (!string.IsNullOrWhiteSpace(userId))
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId));
        if (!string.IsNullOrWhiteSpace(sid))
            identity.AddClaim(new Claim("sid", sid));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };

        return controller;
    }

    private sealed class FakeAuthService : IAuthService
    {
        public Result<AuthResponse> LoginResult { get; set; } = Result<AuthResponse>.Success(new AuthResponse());
        public Result<AuthResponse> RefreshResult { get; set; } = Result<AuthResponse>.Success(new AuthResponse());
        public Result<MeResponse> GetMeResult { get; set; } = Result<MeResponse>.Success(new MeResponse());
        public Result<IReadOnlyList<UserSessionResponse>> GetActiveSessionsResult { get; set; } = Result<IReadOnlyList<UserSessionResponse>>.Success([]);
        public Result<IReadOnlyList<UserSessionResponse>> GetRecentSessionsResult { get; set; } = Result<IReadOnlyList<UserSessionResponse>>.Success([]);
        public Result LogoutResult { get; set; } = Result.Success();
        public Result RevokeSessionResult { get; set; } = Result.Success();
        public Func<Guid, Result>? RevokeSessionById { get; set; }

        public string LastUserIdForGetSessions { get; private set; } = string.Empty;
        public Guid? LastCurrentSessionIdForGetSessions { get; private set; }
        public RefreshTokenRequest LastRefreshRequest { get; private set; } = new();
        public List<Guid> RevokedSessionIds { get; } = [];

        public Task<Result> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
            => Task.FromResult(Result.Success());

        public Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
            => Task.FromResult(LoginResult);

        public Task<Result<AuthResponse>> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
        {
            LastRefreshRequest = request;
            return Task.FromResult(RefreshResult);
        }

        public Task<Result<MeResponse>> GetMeAsync(string userId, CancellationToken cancellationToken)
            => Task.FromResult(GetMeResult);

        public Task<Result<IReadOnlyList<UserSessionResponse>>> GetActiveSessionsAsync(string userId, Guid? currentSessionId, CancellationToken cancellationToken)
        {
            LastUserIdForGetSessions = userId;
            LastCurrentSessionIdForGetSessions = currentSessionId;
            return Task.FromResult(GetActiveSessionsResult);
        }

        public Task<Result<IReadOnlyList<UserSessionResponse>>> GetRecentSessionsAsync(string userId, Guid? currentSessionId, int take, CancellationToken cancellationToken)
            => Task.FromResult(GetRecentSessionsResult);

        public Task<Result> LogoutAsync(string userId, RefreshTokenRequest request, CancellationToken cancellationToken)
            => Task.FromResult(LogoutResult);

        public Task<Result> RevokeSessionAsync(string userId, Guid sessionId, CancellationToken cancellationToken)
        {
            RevokedSessionIds.Add(sessionId);
            return Task.FromResult(RevokeSessionById?.Invoke(sessionId) ?? RevokeSessionResult);
        }

        public Task<Result> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken)
            => Task.FromResult(Result.Success());

        public Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
            => Task.FromResult(Result.Success());

        public Task<Result> ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken cancellationToken)
            => Task.FromResult(Result.Success());

        public Task<Result> ConfirmEmailAsync(string email, string token, CancellationToken cancellationToken)
            => Task.FromResult(Result.Success());

        public Task<Result> ResendConfirmationAsync(string email, CancellationToken cancellationToken)
            => Task.FromResult(Result.Success());

        public Task<Result> UpdateProfileAsync(string userId, UpdateProfileRequest request, CancellationToken cancellationToken)
            => Task.FromResult(Result.Success());
    }
}
