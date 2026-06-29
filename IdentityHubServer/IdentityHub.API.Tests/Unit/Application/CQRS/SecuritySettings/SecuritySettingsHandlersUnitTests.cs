using IdentityHub.Application.Common.Errors;
using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.SecuritySettings.Commands;
using IdentityHub.Application.CQRS.SecuritySettings.Handlers;
using IdentityHub.Application.CQRS.SecuritySettings.Queries;
using IdentityHub.Application.DTOs;
using IdentityHub.Application.Interfaces;
using Xunit;

namespace IdentityHub.API.Tests;

public sealed class SecuritySettingsHandlersUnitTests
{
    [Fact]
    public async Task GetSecuritySettingsQueryHandler_ShouldDelegateToService()
    {
        var expected = Result<SecuritySettingsResponse>.Success(new SecuritySettingsResponse
        {
            AccessTokenMinutes = 15,
            RefreshTokenDays = 7,
            MaxLoginAttempts = 5,
            LockDurationMinutes = 30,
            RequireEmailConfirmation = true
        });

        var service = new FakeSecuritySettingsService
        {
            GetResult = expected
        };

        var handler = new GetSecuritySettingsQueryHandler(service);
        using var cts = new CancellationTokenSource();

        var result = await handler.Handle(new GetSecuritySettingsQuery(), cts.Token);

        Assert.Same(expected, result);
        Assert.Equal(1, service.GetCalls);
        Assert.Equal(cts.Token, service.LastGetCancellationToken);
    }

    [Fact]
    public async Task UpdateSecuritySettingsCommandHandler_ShouldForwardRequestAndToken()
    {
        var expected = Result.Failure(Error.Create("validation", "invalid settings"));

        var service = new FakeSecuritySettingsService
        {
            UpdateResult = expected
        };

        var handler = new UpdateSecuritySettingsCommandHandler(service);
        var request = new UpdateSecuritySettingsRequest
        {
            AccessTokenMinutes = 10,
            RefreshTokenDays = 10,
            MaxLoginAttempts = 3,
            LockDurationMinutes = 15,
            RequireEmailConfirmation = false
        };

        using var cts = new CancellationTokenSource();

        var result = await handler.Handle(new UpdateSecuritySettingsCommand(request), cts.Token);

        Assert.Same(expected, result);
        Assert.Equal(1, service.UpdateCalls);
        Assert.Same(request, service.LastUpdateRequest);
        Assert.Equal(cts.Token, service.LastUpdateCancellationToken);
    }

    private sealed class FakeSecuritySettingsService : ISecuritySettingsService
    {
        public Result<SecuritySettingsResponse> GetResult { get; set; }
            = Result<SecuritySettingsResponse>.Success(new SecuritySettingsResponse());

        public Result UpdateResult { get; set; } = Result.Success();

        public int GetCalls { get; private set; }
        public int UpdateCalls { get; private set; }
        public CancellationToken LastGetCancellationToken { get; private set; }
        public CancellationToken LastUpdateCancellationToken { get; private set; }
        public UpdateSecuritySettingsRequest? LastUpdateRequest { get; private set; }

        public Task<Result<SecuritySettingsResponse>> GetSettingsAsync(CancellationToken cancellationToken = default)
        {
            GetCalls++;
            LastGetCancellationToken = cancellationToken;
            return Task.FromResult(GetResult);
        }

        public Task<Result> UpdateSettingsAsync(UpdateSecuritySettingsRequest request, CancellationToken cancellationToken = default)
        {
            UpdateCalls++;
            LastUpdateRequest = request;
            LastUpdateCancellationToken = cancellationToken;
            return Task.FromResult(UpdateResult);
        }
    }
}
