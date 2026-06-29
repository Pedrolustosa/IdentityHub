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

public sealed class AuthControllerCoverageUnitTests
{
    [Fact]
    public async Task Register_ShouldMapServiceResult()
    {
        var service = new FakeAuthService { RegisterResult = Result.Success() };
        var controller = CreateController(service);

        var action = await controller.Register(new RegisterRequest(), CancellationToken.None);

        Assert.IsType<OkResult>(action);
    }

    [Fact]
    public async Task ConfirmEmail_ShouldMapFailureToUnauthorized()
    {
        var service = new FakeAuthService
        {
            ConfirmEmailResult = Result.Failure(Error.Create("Auth.InvalidToken", "invalid"))
        };
        var controller = CreateController(service);

        var action = await controller.ConfirmEmail("user@x.com", "bad", CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(action);
        Assert.Equal(StatusCodes.Status401Unauthorized, objectResult.StatusCode);
    }

    [Fact]
    public async Task ResendConfirmation_ShouldPassEmailToService()
    {
        var service = new FakeAuthService { ResendConfirmationResult = Result.Success() };
        var controller = CreateController(service);

        var action = await controller.ResendConfirmation(new ForgotPasswordRequest { Email = "u@x.com" }, CancellationToken.None);

        Assert.IsType<OkResult>(action);
        Assert.Equal("u@x.com", service.LastResendEmail);
    }

    [Fact]
    public async Task GetRecentSessions_ShouldReturnUnauthorized_WhenUserIdClaimMissing()
    {
        var controller = CreateController(new FakeAuthService());

        var action = await controller.GetRecentSessions(20, CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(action);
    }

    [Fact]
    public async Task GetSessions_ShouldReturnUnauthorized_WhenUserIdClaimMissing()
    {
        var controller = CreateController(new FakeAuthService());

        var action = await controller.GetSessions(CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(action);
    }

    [Fact]
    public async Task GetRecentSessions_ShouldPassSid_WhenAvailable()
    {
        var sid = Guid.NewGuid();
        var service = new FakeAuthService
        {
            GetRecentSessionsResult = Result<IReadOnlyList<UserSessionResponse>>.Success([])
        };
        var controller = CreateController(service, userId: "u1", sid: sid.ToString());

        var action = await controller.GetRecentSessions(12, CancellationToken.None);

        Assert.IsType<OkObjectResult>(action);
        Assert.Equal("u1", service.LastRecentSessionsUserId);
        Assert.Equal(sid, service.LastRecentSessionsCurrentSessionId);
        Assert.Equal(12, service.LastRecentSessionsTake);
    }

    [Fact]
    public async Task RevokeSession_ShouldReturnUnauthorized_WhenUserIdClaimMissing()
    {
        var controller = CreateController(new FakeAuthService());

        var action = await controller.RevokeSession(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(action);
    }

    [Fact]
    public async Task RevokeSession_ShouldCallService_WhenUserIdPresent()
    {
        var service = new FakeAuthService { RevokeSessionResult = Result.Success() };
        var controller = CreateController(service, userId: "u1");
        var sessionId = Guid.NewGuid();

        var action = await controller.RevokeSession(sessionId, CancellationToken.None);

        Assert.IsType<OkResult>(action);
        Assert.Equal("u1", service.LastRevokeUserId);
        Assert.Equal(sessionId, service.LastRevokeSessionId);
    }

    [Fact]
    public async Task RevokeOtherSessions_ShouldReturnFailure_WhenGetSessionsFails()
    {
        var service = new FakeAuthService
        {
            GetActiveSessionsResult = Result<IReadOnlyList<UserSessionResponse>>.Failure(Error.Create("Auth.Forbidden", "blocked"))
        };
        var controller = CreateController(service, userId: "u1");

        var action = await controller.RevokeOtherSessions(CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(action);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task RevokeOtherSessions_ShouldReturnFailure_WhenRevokeFails()
    {
        var session = new UserSessionResponse { Id = Guid.NewGuid(), IsCurrent = false };
        var service = new FakeAuthService
        {
            GetActiveSessionsResult = Result<IReadOnlyList<UserSessionResponse>>.Success([session]),
            RevokeSessionResult = Result.Failure(Error.Create("Auth.Forbidden", "blocked"))
        };
        var controller = CreateController(service, userId: "u1");

        var action = await controller.RevokeOtherSessions(CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(action);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task RevokeUserSessions_ShouldReturnNoContent_WhenAllRevokesSucceed()
    {
        var session = new UserSessionResponse { Id = Guid.NewGuid(), IsCurrent = false };
        var service = new FakeAuthService
        {
            GetActiveSessionsResult = Result<IReadOnlyList<UserSessionResponse>>.Success([session]),
            RevokeSessionResult = Result.Success()
        };
        var controller = CreateController(service, userId: "admin");

        var action = await controller.RevokeUserSessions("target", CancellationToken.None);

        Assert.IsType<NoContentResult>(action);
    }

    [Fact]
    public async Task RevokeOtherSessions_ShouldReturnUnauthorized_WhenUserIdClaimMissing()
    {
        var controller = CreateController(new FakeAuthService());

        var action = await controller.RevokeOtherSessions(CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(action);
    }

    [Fact]
    public async Task Logout_ShouldReturnUnauthorized_WhenUserIdClaimMissing()
    {
        var controller = CreateController(new FakeAuthService());

        var action = await controller.Logout(new RefreshTokenRequest(), CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(action);
    }

    [Fact]
    public async Task Logout_ShouldResolveEmptyToken_WhenRequestIsNull()
    {
        var service = new FakeAuthService { LogoutResult = Result.Success() };
        var controller = CreateController(service, userId: "u1");

        var action = await controller.Logout(request: null, CancellationToken.None);

        Assert.IsType<OkResult>(action);
        Assert.Equal(string.Empty, service.LastLogoutRefreshToken);
    }

    [Fact]
    public async Task ForgotPassword_And_ResetPassword_ShouldMapServiceResults()
    {
        var service = new FakeAuthService
        {
            ForgotPasswordResult = Result.Success(),
            ResetPasswordResult = Result.Success()
        };
        var controller = CreateController(service);

        var forgot = await controller.ForgotPassword(new ForgotPasswordRequest { Email = "x@y.com" }, CancellationToken.None);
        var reset = await controller.ResetPassword(new ResetPasswordRequest(), CancellationToken.None);

        Assert.IsType<OkResult>(forgot);
        Assert.IsType<OkResult>(reset);
    }

    [Fact]
    public async Task ChangePassword_ShouldReturnUnauthorized_WhenUserIdClaimMissing()
    {
        var controller = CreateController(new FakeAuthService());

        var action = await controller.ChangePassword(new ChangePasswordRequest(), CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(action);
    }

    [Fact]
    public async Task UpdateProfile_ShouldReturnUnauthorized_WhenUserIdClaimMissing()
    {
        var controller = CreateController(new FakeAuthService());

        var action = await controller.UpdateProfile(new UpdateProfileRequest(), CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(action);
    }

    [Fact]
    public async Task UpdateProfile_ShouldCallService_WhenUserIdClaimPresent()
    {
        var service = new FakeAuthService { UpdateProfileResult = Result.Success() };
        var controller = CreateController(service, userId: "u42");

        var action = await controller.UpdateProfile(new UpdateProfileRequest(), CancellationToken.None);

        Assert.IsType<OkResult>(action);
        Assert.Equal("u42", service.LastUpdateProfileUserId);
    }

    [Fact]
    public async Task Refresh_ShouldPassEmptyToken_WhenNoRequestAndNoCookie()
    {
        var service = new FakeAuthService
        {
            RefreshResult = Result<AuthResponse>.Success(new AuthResponse { Token = "access", RefreshToken = "" })
        };
        var controller = CreateController(service);

        var action = await controller.Refresh(request: null, CancellationToken.None);

        Assert.IsType<OkObjectResult>(action);
        Assert.Equal(string.Empty, service.LastRefreshTokenValue);

        var setCookie = controller.ControllerContext.HttpContext.Response.Headers.SetCookie.ToString();
        Assert.DoesNotContain("ih_refresh=", setCookie, StringComparison.OrdinalIgnoreCase);
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
        public Result RegisterResult { get; set; } = Result.Success();
        public Result<AuthResponse> LoginResult { get; set; } = Result<AuthResponse>.Success(new AuthResponse());
        public Result<AuthResponse> RefreshResult { get; set; } = Result<AuthResponse>.Success(new AuthResponse());
        public Result<MeResponse> GetMeResult { get; set; } = Result<MeResponse>.Success(new MeResponse());
        public Result<IReadOnlyList<UserSessionResponse>> GetActiveSessionsResult { get; set; } = Result<IReadOnlyList<UserSessionResponse>>.Success([]);
        public Result<IReadOnlyList<UserSessionResponse>> GetRecentSessionsResult { get; set; } = Result<IReadOnlyList<UserSessionResponse>>.Success([]);
        public Result LogoutResult { get; set; } = Result.Success();
        public Result RevokeSessionResult { get; set; } = Result.Success();
        public Result ForgotPasswordResult { get; set; } = Result.Success();
        public Result ResetPasswordResult { get; set; } = Result.Success();
        public Result ChangePasswordResult { get; set; } = Result.Success();
        public Result ConfirmEmailResult { get; set; } = Result.Success();
        public Result ResendConfirmationResult { get; set; } = Result.Success();
        public Result UpdateProfileResult { get; set; } = Result.Success();

        public string LastResendEmail { get; private set; } = string.Empty;
        public string LastRevokeUserId { get; private set; } = string.Empty;
        public Guid LastRevokeSessionId { get; private set; }
        public string LastUpdateProfileUserId { get; private set; } = string.Empty;
        public string LastRefreshTokenValue { get; private set; } = string.Empty;
        public string LastLogoutRefreshToken { get; private set; } = string.Empty;
        public string LastRecentSessionsUserId { get; private set; } = string.Empty;
        public Guid? LastRecentSessionsCurrentSessionId { get; private set; }
        public int LastRecentSessionsTake { get; private set; }

        public Task<Result> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
            => Task.FromResult(RegisterResult);

        public Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
            => Task.FromResult(LoginResult);

        public Task<Result<AuthResponse>> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
        {
            LastRefreshTokenValue = request.RefreshToken;
            return Task.FromResult(RefreshResult);
        }

        public Task<Result<MeResponse>> GetMeAsync(string userId, CancellationToken cancellationToken)
            => Task.FromResult(GetMeResult);

        public Task<Result<IReadOnlyList<UserSessionResponse>>> GetActiveSessionsAsync(string userId, Guid? currentSessionId, CancellationToken cancellationToken)
            => Task.FromResult(GetActiveSessionsResult);

        public Task<Result<IReadOnlyList<UserSessionResponse>>> GetRecentSessionsAsync(string userId, Guid? currentSessionId, int take, CancellationToken cancellationToken)
        {
            LastRecentSessionsUserId = userId;
            LastRecentSessionsCurrentSessionId = currentSessionId;
            LastRecentSessionsTake = take;
            return Task.FromResult(GetRecentSessionsResult);
        }

        public Task<Result> LogoutAsync(string userId, RefreshTokenRequest request, CancellationToken cancellationToken)
        {
            LastLogoutRefreshToken = request.RefreshToken;
            return Task.FromResult(LogoutResult);
        }

        public Task<Result> RevokeSessionAsync(string userId, Guid sessionId, CancellationToken cancellationToken)
        {
            LastRevokeUserId = userId;
            LastRevokeSessionId = sessionId;
            return Task.FromResult(RevokeSessionResult);
        }

        public Task<Result> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken)
            => Task.FromResult(ForgotPasswordResult);

        public Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
            => Task.FromResult(ResetPasswordResult);

        public Task<Result> ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken cancellationToken)
            => Task.FromResult(ChangePasswordResult);

        public Task<Result> ConfirmEmailAsync(string email, string token, CancellationToken cancellationToken)
            => Task.FromResult(ConfirmEmailResult);

        public Task<Result> ResendConfirmationAsync(string email, CancellationToken cancellationToken)
        {
            LastResendEmail = email;
            return Task.FromResult(ResendConfirmationResult);
        }

        public Task<Result> UpdateProfileAsync(string userId, UpdateProfileRequest request, CancellationToken cancellationToken)
        {
            LastUpdateProfileUserId = userId;
            return Task.FromResult(UpdateProfileResult);
        }
    }
}
