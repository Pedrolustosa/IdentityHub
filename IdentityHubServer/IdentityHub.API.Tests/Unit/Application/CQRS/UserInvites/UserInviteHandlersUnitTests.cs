using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.UserInvites.Commands;
using IdentityHub.Application.CQRS.UserInvites.Handlers;
using IdentityHub.Application.CQRS.UserInvites.Queries;
using IdentityHub.Application.DTOs;
using IdentityHub.Application.Interfaces;
using Xunit;

namespace IdentityHub.API.Tests;

public sealed class UserInviteHandlersUnitTests
{
    [Fact]
    public async Task GetUserInvitesQueryHandler_ShouldForwardPaginationToService()
    {
        var expected = Result<UserInvitesPagedResponse>.Success(new UserInvitesPagedResponse
        {
            Page = 2,
            PageSize = 5,
            TotalCount = 12,
            TotalPages = 3,
            Items =
            [
                new UserInviteResponse
                {
                    Id = Guid.NewGuid(),
                    Email = "test@identityhub.com",
                    Status = "Pending"
                }
            ]
        });

        var service = new FakeUserInvitesService
        {
            GetResult = expected
        };

        var handler = new GetUserInvitesQueryHandler(service);
        using var cts = new CancellationTokenSource();

        var result = await handler.Handle(new GetUserInvitesQuery(2, 5), cts.Token);

        Assert.Same(expected, result);
        Assert.Equal(1, service.GetCalls);
        Assert.Equal(2, service.LastPage);
        Assert.Equal(5, service.LastPageSize);
        Assert.Equal(cts.Token, service.LastGetCancellationToken);
    }

    [Fact]
    public async Task ResendUserInviteCommandHandler_ShouldForwardInviteId()
    {
        var expected = Result.Success();
        var service = new FakeUserInvitesService
        {
            ResendResult = expected
        };

        var handler = new ResendUserInviteCommandHandler(service);
        var inviteId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();

        var result = await handler.Handle(new ResendUserInviteCommand(inviteId), cts.Token);

        Assert.Same(expected, result);
        Assert.Equal(1, service.ResendCalls);
        Assert.Equal(inviteId, service.LastResendInviteId);
        Assert.Equal(cts.Token, service.LastResendCancellationToken);
    }

    [Fact]
    public async Task CancelUserInviteCommandHandler_ShouldForwardInviteId()
    {
        var expected = Result.Success();
        var service = new FakeUserInvitesService
        {
            CancelResult = expected
        };

        var handler = new CancelUserInviteCommandHandler(service);
        var inviteId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();

        var result = await handler.Handle(new CancelUserInviteCommand(inviteId), cts.Token);

        Assert.Same(expected, result);
        Assert.Equal(1, service.CancelCalls);
        Assert.Equal(inviteId, service.LastCancelInviteId);
        Assert.Equal(cts.Token, service.LastCancelCancellationToken);
    }

    private sealed class FakeUserInvitesService : IUserInvitesService
    {
        public Result<UserInvitesPagedResponse> GetResult { get; set; }
            = Result<UserInvitesPagedResponse>.Success(new UserInvitesPagedResponse());

        public Result ResendResult { get; set; } = Result.Success();
        public Result CancelResult { get; set; } = Result.Success();

        public int GetCalls { get; private set; }
        public int ResendCalls { get; private set; }
        public int CancelCalls { get; private set; }

        public int LastPage { get; private set; }
        public int LastPageSize { get; private set; }
        public Guid LastResendInviteId { get; private set; }
        public Guid LastCancelInviteId { get; private set; }

        public CancellationToken LastGetCancellationToken { get; private set; }
        public CancellationToken LastResendCancellationToken { get; private set; }
        public CancellationToken LastCancelCancellationToken { get; private set; }

        public Task<Result<UserInvitesPagedResponse>> GetUserInvitesAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
        {
            GetCalls++;
            LastPage = page;
            LastPageSize = pageSize;
            LastGetCancellationToken = cancellationToken;
            return Task.FromResult(GetResult);
        }

        public Task<Result> ResendUserInviteAsync(Guid inviteId, CancellationToken cancellationToken = default)
        {
            ResendCalls++;
            LastResendInviteId = inviteId;
            LastResendCancellationToken = cancellationToken;
            return Task.FromResult(ResendResult);
        }

        public Task<Result> CancelUserInviteAsync(Guid inviteId, CancellationToken cancellationToken = default)
        {
            CancelCalls++;
            LastCancelInviteId = inviteId;
            LastCancelCancellationToken = cancellationToken;
            return Task.FromResult(CancelResult);
        }
    }
}
