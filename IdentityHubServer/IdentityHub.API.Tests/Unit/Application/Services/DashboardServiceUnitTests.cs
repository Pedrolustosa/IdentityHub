using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.Dashboard.Queries;
using IdentityHub.Application.DTOs;
using IdentityHub.Application.Services;
using MediatR;
using Xunit;

namespace IdentityHub.API.Tests;

public sealed class DashboardServiceUnitTests
{
    [Fact]
    public async Task GetAsync_ShouldSendGetDashboardQuery_AndReturnSenderResult()
    {
        var expected = Result<DashboardResponse>.Success(new DashboardResponse
        {
            TotalUsers = 10,
            ActiveUsers = 8
        });

        var sender = new FakeSender(expected);
        var service = new DashboardService(sender);

        var result = await service.GetAsync(CancellationToken.None);

        Assert.Same(expected, result);
        Assert.NotNull(sender.LastRequest);
        Assert.IsType<GetDashboardQuery>(sender.LastRequest);
    }

    private sealed class FakeSender : ISender
    {
        private readonly Result<DashboardResponse> _result;

        public FakeSender(Result<DashboardResponse> result)
        {
            _result = result;
        }

        public object? LastRequest { get; private set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult((TResponse)(object)_result);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
        {
            LastRequest = request;
            return Task.CompletedTask;
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult<object?>(_result);
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return AsyncEnumerable.Empty<TResponse>();
        }

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return AsyncEnumerable.Empty<object?>();
        }
    }
}
