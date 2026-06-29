using IdentityHub.API.Controllers;
using IdentityHub.Application.Common.Errors;
using IdentityHub.Application.Common.Results;
using IdentityHub.Application.DTOs;
using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Xunit;

namespace IdentityHub.API.Tests;

public sealed class UsersAndRolesControllersUnitTests
{
    [Fact]
    public async Task UsersController_Create_ShouldMapAuthErrorToUnauthorized()
    {
        var userService = new FakeUserService
        {
            CreateResult = Result.Failure(Error.Create("Auth.InvalidCredentials", "invalid"))
        };

        var controller = CreateUsersController(userService, new FakeAuthService(), new FakeAuditLogService());

        var action = await controller.Create(new CreateUserRequest(), CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(action);
        Assert.Equal(StatusCodes.Status401Unauthorized, objectResult.StatusCode);
    }

    [Fact]
    public async Task UsersController_Update_ShouldReturnOk_WhenServiceSucceeds()
    {
        var userService = new FakeUserService { UpdateResult = Result.Success() };
        var controller = CreateUsersController(userService, new FakeAuthService(), new FakeAuditLogService());

        var action = await controller.Update("user-id", new UpdateUserRequest(), CancellationToken.None);

        Assert.IsType<OkResult>(action);
        Assert.Equal("user-id", userService.LastUpdatedUserId);
    }

    [Fact]
    public async Task UsersController_Invite_ShouldReturnOk_WhenServiceSucceeds()
    {
        var userService = new FakeUserService { InviteResult = Result.Success() };
        var controller = CreateUsersController(userService, new FakeAuthService(), new FakeAuditLogService());

        var action = await controller.Invite(new InviteUserRequest(), CancellationToken.None);

        Assert.IsType<OkResult>(action);
    }

    [Fact]
    public async Task UsersController_UpdateRoles_ShouldMapNotFoundTo404()
    {
        var userService = new FakeUserService
        {
            UpdateRolesResult = Result.Failure(Error.Create("User.NotFound", "missing"))
        };
        var controller = CreateUsersController(userService, new FakeAuthService(), new FakeAuditLogService());

        var action = await controller.UpdateRoles("missing", new UpdateRolesRequest(), CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(action);
        Assert.Equal(StatusCodes.Status404NotFound, objectResult.StatusCode);
    }

    [Fact]
    public async Task UsersController_GetSessionsByUser_ShouldPassCurrentSessionId_WhenSameUserAndSidValid()
    {
        var sid = Guid.NewGuid();
        var authService = new FakeAuthService
        {
            RecentSessionsResult = Result<IReadOnlyList<UserSessionResponse>>.Success([])
        };

        var controller = CreateUsersController(new FakeUserService(), authService, new FakeAuditLogService(), "same-user", sid.ToString());

        var action = await controller.GetSessionsByUser("same-user", take: 10, CancellationToken.None);

        Assert.IsType<OkObjectResult>(action);
        Assert.Equal("same-user", authService.LastRecentSessionsUserId);
        Assert.Equal(sid, authService.LastRecentSessionsCurrentSessionId);
        Assert.Equal(10, authService.LastRecentSessionsTake);
    }

    [Fact]
    public async Task UsersController_GetSessionsByUser_ShouldPassNullCurrentSessionId_WhenDifferentUser()
    {
        var authService = new FakeAuthService
        {
            RecentSessionsResult = Result<IReadOnlyList<UserSessionResponse>>.Success([])
        };

        var controller = CreateUsersController(new FakeUserService(), authService, new FakeAuditLogService(), "requester", Guid.NewGuid().ToString());

        var action = await controller.GetSessionsByUser("target", take: 20, CancellationToken.None);

        Assert.IsType<OkObjectResult>(action);
        Assert.Equal("target", authService.LastRecentSessionsUserId);
        Assert.Null(authService.LastRecentSessionsCurrentSessionId);
    }

    [Fact]
    public async Task RolesController_Create_ShouldReturnOk_WhenServiceSucceeds()
    {
        var roleService = new FakeRoleService { CreateResult = Result.Success() };
        var controller = new RolesController(roleService);

        var action = await controller.Create(new CreateRoleRequest(), CancellationToken.None);

        Assert.IsType<OkResult>(action);
    }

    [Fact]
    public async Task RolesController_UpdatePermissions_ShouldPassPermissionsToService_AndReturnOk()
    {
        var roleService = new FakeRoleService { UpdatePermissionsResult = Result.Success() };
        var controller = new RolesController(roleService);

        var request = new UpdateRolePermissionsRequest
        {
            Permissions = ["Users.View", "Roles.Update"]
        };

        var action = await controller.UpdatePermissions("role-1", request, CancellationToken.None);

        Assert.IsType<OkResult>(action);
        Assert.Equal("role-1", roleService.LastUpdatePermissionsRoleId);
        Assert.Equal(2, roleService.LastUpdatePermissions.Count);
        Assert.Contains("Users.View", roleService.LastUpdatePermissions);
        Assert.Contains("Roles.Update", roleService.LastUpdatePermissions);
    }

    [Fact]
    public async Task RolesController_Delete_ShouldMapNotFoundTo404()
    {
        var roleService = new FakeRoleService
        {
            DeleteResult = Result.Failure(Error.Create("Role.NotFound", "missing"))
        };
        var controller = new RolesController(roleService);

        var action = await controller.Delete("role-missing", CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(action);
        Assert.Equal(StatusCodes.Status404NotFound, objectResult.StatusCode);
    }

    [Fact]
    public async Task RolesController_GetById_ShouldReturnOkObject_WhenServiceSucceeds()
    {
        var roleService = new FakeRoleService
        {
            GetByIdResult = Result<RoleResponse>.Success(new RoleResponse { Id = "role-1", Name = "Admin" })
        };
        var controller = new RolesController(roleService);

        var action = await controller.GetById("role-1", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action);
        var payload = Assert.IsType<RoleResponse>(ok.Value);
        Assert.Equal("role-1", payload.Id);
    }

    [Fact]
    public async Task RolesController_GetPermissionCatalog_ShouldReturnOkObject_WhenServiceSucceeds()
    {
        var roleService = new FakeRoleService
        {
            PermissionCatalogResult = Result<List<string>>.Success(["Users.View", "Roles.View"])
        };
        var controller = new RolesController(roleService);

        var action = await controller.GetPermissionCatalog(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action);
        var payload = Assert.IsType<List<string>>(ok.Value);
        Assert.Equal(2, payload.Count);
    }

    [Fact]
    public async Task RolesController_GetPermissions_ShouldReturnOkObject_WhenServiceSucceeds()
    {
        var roleService = new FakeRoleService
        {
            PermissionsResult = Result<List<string>>.Success(["Users.View", "Users.Update"])
        };
        var controller = new RolesController(roleService);

        var action = await controller.GetPermissions("role-1", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action);
        var payload = Assert.IsType<List<string>>(ok.Value);
        Assert.Equal(2, payload.Count);
    }

    private static UsersController CreateUsersController(
        IUserService userService,
        IAuthService authService,
        IAuditLogService auditLogService,
        string? userId = null,
        string? sid = null)
    {
        var controller = new UsersController(userService, authService, auditLogService);

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

    private sealed class FakeUserService : IUserService
    {
        public Result<List<UserResponse>> GetAllResult { get; set; } = Result<List<UserResponse>>.Success([]);
        public Result<UserResponse> GetByIdResult { get; set; } = Result<UserResponse>.Success(new UserResponse());
        public Result CreateResult { get; set; } = Result.Success();
        public Result InviteResult { get; set; } = Result.Success();
        public Result UpdateResult { get; set; } = Result.Success();
        public Result DeleteResult { get; set; } = Result.Success();
        public Result UpdateRolesResult { get; set; } = Result.Success();

        public string LastUpdatedUserId { get; private set; } = string.Empty;

        public Task<Result<List<UserResponse>>> GetAllAsync(CancellationToken cancellationToken)
            => Task.FromResult(GetAllResult);

        public Task<Result<UserResponse>> GetByIdAsync(string id, CancellationToken cancellationToken)
            => Task.FromResult(GetByIdResult);

        public Task<Result> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
            => Task.FromResult(CreateResult);

        public Task<Result> InviteAsync(InviteUserRequest request, CancellationToken cancellationToken)
            => Task.FromResult(InviteResult);

        public Task<Result> UpdateAsync(string id, UpdateUserRequest request, CancellationToken cancellationToken)
        {
            LastUpdatedUserId = id;
            return Task.FromResult(UpdateResult);
        }

        public Task<Result> DeleteAsync(string id, CancellationToken cancellationToken)
            => Task.FromResult(DeleteResult);

        public Task<Result> UpdateRolesAsync(string id, UpdateRolesRequest request, CancellationToken cancellationToken)
            => Task.FromResult(UpdateRolesResult);
    }

    private sealed class FakeRoleService : IRoleService
    {
        public Result<List<RoleResponse>> GetAllResult { get; set; } = Result<List<RoleResponse>>.Success([]);
        public Result<RoleResponse> GetByIdResult { get; set; } = Result<RoleResponse>.Success(new RoleResponse());
        public Result CreateResult { get; set; } = Result.Success();
        public Result UpdateResult { get; set; } = Result.Success();
        public Result DeleteResult { get; set; } = Result.Success();
        public Result<List<string>> PermissionCatalogResult { get; set; } = Result<List<string>>.Success([]);
        public Result<List<string>> PermissionsResult { get; set; } = Result<List<string>>.Success([]);
        public Result UpdatePermissionsResult { get; set; } = Result.Success();

        public string LastUpdatePermissionsRoleId { get; private set; } = string.Empty;
        public List<string> LastUpdatePermissions { get; private set; } = [];

        public Task<Result<List<RoleResponse>>> GetAllAsync(CancellationToken cancellationToken)
            => Task.FromResult(GetAllResult);

        public Task<Result<RoleResponse>> GetByIdAsync(string id, CancellationToken cancellationToken)
            => Task.FromResult(GetByIdResult);

        public Task<Result> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken)
            => Task.FromResult(CreateResult);

        public Task<Result> UpdateAsync(string id, UpdateRoleRequest request, CancellationToken cancellationToken)
            => Task.FromResult(UpdateResult);

        public Task<Result> DeleteAsync(string id, CancellationToken cancellationToken)
            => Task.FromResult(DeleteResult);

        public Task<Result<List<string>>> GetPermissionCatalogAsync(CancellationToken cancellationToken)
            => Task.FromResult(PermissionCatalogResult);

        public Task<Result<List<string>>> GetPermissionsAsync(string roleId, CancellationToken cancellationToken)
            => Task.FromResult(PermissionsResult);

        public Task<Result> UpdatePermissionsAsync(string roleId, List<string> permissions, CancellationToken cancellationToken)
        {
            LastUpdatePermissionsRoleId = roleId;
            LastUpdatePermissions = permissions;
            return Task.FromResult(UpdatePermissionsResult);
        }
    }

    private sealed class FakeAuditLogService : IAuditLogService
    {
        public Task<Result<PagedResponse<AuditLogItemResponse>>> GetPagedAsync(AuditLogFilter request, int page, int pageSize, CancellationToken cancellationToken)
            => Task.FromResult(Result<PagedResponse<AuditLogItemResponse>>.Success(new PagedResponse<AuditLogItemResponse>()));

        public Task<Result<AuditLogItemResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(Result<AuditLogItemResponse>.Success(new AuditLogItemResponse()));

        public Task<Result<IReadOnlyList<AuditLogItemResponse>>> GetRecentByUserAsync(string userId, int take, CancellationToken cancellationToken)
            => Task.FromResult(Result<IReadOnlyList<AuditLogItemResponse>>.Success([]));

        public Task<string> ExportCsvAsync(AuditLogFilter request, CancellationToken cancellationToken)
            => Task.FromResult(string.Empty);
    }

    private sealed class FakeAuthService : IAuthService
    {
        public Result<IReadOnlyList<UserSessionResponse>> RecentSessionsResult { get; set; } = Result<IReadOnlyList<UserSessionResponse>>.Success([]);

        public string LastRecentSessionsUserId { get; private set; } = string.Empty;
        public Guid? LastRecentSessionsCurrentSessionId { get; private set; }
        public int LastRecentSessionsTake { get; private set; }

        public Task<Result> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
            => Task.FromResult(Result.Success());

        public Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
            => Task.FromResult(Result<AuthResponse>.Success(new AuthResponse()));

        public Task<Result<AuthResponse>> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
            => Task.FromResult(Result<AuthResponse>.Success(new AuthResponse()));

        public Task<Result<MeResponse>> GetMeAsync(string userId, CancellationToken cancellationToken)
            => Task.FromResult(Result<MeResponse>.Success(new MeResponse()));

        public Task<Result<IReadOnlyList<UserSessionResponse>>> GetActiveSessionsAsync(string userId, Guid? currentSessionId, CancellationToken cancellationToken)
            => Task.FromResult(Result<IReadOnlyList<UserSessionResponse>>.Success([]));

        public Task<Result<IReadOnlyList<UserSessionResponse>>> GetRecentSessionsAsync(string userId, Guid? currentSessionId, int take, CancellationToken cancellationToken)
        {
            LastRecentSessionsUserId = userId;
            LastRecentSessionsCurrentSessionId = currentSessionId;
            LastRecentSessionsTake = take;
            return Task.FromResult(RecentSessionsResult);
        }

        public Task<Result> LogoutAsync(string userId, RefreshTokenRequest request, CancellationToken cancellationToken)
            => Task.FromResult(Result.Success());

        public Task<Result> RevokeSessionAsync(string userId, Guid sessionId, CancellationToken cancellationToken)
            => Task.FromResult(Result.Success());

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
