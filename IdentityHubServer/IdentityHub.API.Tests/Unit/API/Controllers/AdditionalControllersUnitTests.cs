using IdentityHub.API.Controllers;
using IdentityHub.Application.Common.Errors;
using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.SecuritySettings.Commands;
using IdentityHub.Application.CQRS.SecuritySettings.Queries;
using IdentityHub.Application.CQRS.UserInvites.Commands;
using IdentityHub.Application.CQRS.UserInvites.Queries;
using IdentityHub.Application.DTOs;
using IdentityHub.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace IdentityHub.API.Tests;

public sealed class AdditionalControllersUnitTests
{
    [Fact]
    public async Task UserInvitesController_GetPaged_ShouldReturnOkObject_WhenMediatorSucceeds()
    {
        var response = new UserInvitesPagedResponse
        {
            Page = 1,
            PageSize = 10,
            TotalCount = 1,
            TotalPages = 1,
            Items =
            [
                new UserInviteResponse { Id = Guid.NewGuid(), Email = "invite@identityhub.com", Status = "Pending" }
            ]
        };

        var mediator = new FakeMediator
        {
            OnSendObject = request => request switch
            {
                GetUserInvitesQuery => Task.FromResult<object?>(Result<UserInvitesPagedResponse>.Success(response)),
                _ => Task.FromResult<object?>(null)
            }
        };

        var controller = new UserInvitesController(mediator);

        var action = await controller.GetPaged(page: 1, pageSize: 10, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action);
        var payload = Assert.IsType<UserInvitesPagedResponse>(ok.Value);
        Assert.Equal(1, payload.TotalCount);
    }

    [Fact]
    public async Task UserInvitesController_ResendInvite_ShouldReturnBadRequest_WhenIdIsInvalid()
    {
        var controller = new UserInvitesController(new FakeMediator());

        var action = await controller.ResendInvite("invalid-guid", CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(action);
        Assert.Equal("Invalid invite ID format", badRequest.Value);
    }

    [Fact]
    public async Task UserInvitesController_CancelInvite_ShouldMapFailureToBadRequest()
    {
        var mediator = new FakeMediator
        {
            OnSendObject = request => request switch
            {
                CancelUserInviteCommand => Task.FromResult<object?>(Result.Failure(Error.Create("Invite.NotFound", "not found"))),
                _ => Task.FromResult<object?>(null)
            }
        };

        var controller = new UserInvitesController(mediator);

        var action = await controller.CancelInvite(Guid.NewGuid().ToString(), CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(action);
        Assert.Equal(404, objectResult.StatusCode);
    }

    [Fact]
    public async Task SecuritySettingsController_Get_ShouldReturnOkObject_WhenMediatorSucceeds()
    {
        var mediator = new FakeMediator
        {
            OnSendObject = request => request switch
            {
                GetSecuritySettingsQuery => Task.FromResult<object?>(Result<SecuritySettingsResponse>.Success(new SecuritySettingsResponse
                {
                    AccessTokenMinutes = 30,
                    RefreshTokenDays = 7,
                    MaxLoginAttempts = 5,
                    LockDurationMinutes = 15,
                    RequireEmailConfirmation = true
                })),
                _ => Task.FromResult<object?>(null)
            }
        };

        var controller = new SecuritySettingsController(mediator);

        var action = await controller.Get(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action);
        Assert.IsType<SecuritySettingsResponse>(ok.Value);
    }

    [Fact]
    public async Task SecuritySettingsController_Update_ShouldReturnBadRequest_WhenRequestIsNull()
    {
        var controller = new SecuritySettingsController(new FakeMediator());

        var action = await controller.Update(request: null!, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(action);
        Assert.Equal("Settings request is required", badRequest.Value);
    }

    [Fact]
    public async Task DashboardController_Get_ShouldReturnOkObject_WhenServiceSucceeds()
    {
        var service = new FakeDashboardService
        {
            Result = Result<DashboardResponse>.Success(new DashboardResponse
            {
                TotalUsers = 100,
                ActiveUsers = 80
            })
        };

        var controller = new DashboardController(service);

        var action = await controller.Get(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action);
        var payload = Assert.IsType<DashboardResponse>(ok.Value);
        Assert.Equal(100, payload.TotalUsers);
    }

    private sealed class FakeDashboardService : IDashboardService
    {
        public Result<DashboardResponse> Result { get; set; } = Result<DashboardResponse>.Success(new DashboardResponse());

        public Task<Result<DashboardResponse>> GetAsync(CancellationToken cancellationToken)
            => Task.FromResult(Result);
    }

    private sealed class FakeMediator : IMediator
    {
        public Func<object, Task<object?>> OnSendObject { get; set; }
            = _ => Task.FromResult<object?>(null);

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
            => OnSendObject(request).ContinueWith(task => (TResponse)task.Result!, cancellationToken);

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
            => Task.CompletedTask;

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
            => OnSendObject(request);

        public Task Publish(object notification, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
            => Task.CompletedTask;

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => AsyncEnumerable.Empty<TResponse>();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
            => AsyncEnumerable.Empty<object?>();
    }
}
