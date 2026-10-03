using IdentityHub.Application.Common.Results;
using IdentityHub.Application.DTOs;
using IdentityHub.Application.Interfaces;
using IdentityHub.Application.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace IdentityHub.API.Tests;

public sealed class TokenServiceUnitTests
{
    private readonly TokenService _service;

    public TokenServiceUnitTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "integration-tests-jwt-key-with-at-least-32-bytes",
                ["Jwt:Issuer"] = "IdentityHub",
                ["Jwt:Audience"] = "IdentityHubUsers",
                ["Jwt:ExpireMinutes"] = "60"
            })
            .Build();

        _service = new TokenService(configuration, new FakeSecuritySettingsService());
    }

    private sealed class FakeSecuritySettingsService : ISecuritySettingsService
    {
        public Task<Result<SecuritySettingsResponse>> GetSettingsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Result<SecuritySettingsResponse>.Success(new SecuritySettingsResponse
            {
                AccessTokenMinutes = 30,
                RefreshTokenDays = 7,
                MaxLoginAttempts = 5,
                LockDurationMinutes = 15,
                RequireEmailConfirmation = true
            }));

        public Task<Result> UpdateSettingsAsync(UpdateSecuritySettingsRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Success());
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnValidBase64String()
    {
        var refreshToken = _service.GenerateRefreshToken();

        Assert.False(string.IsNullOrWhiteSpace(refreshToken));

        var bytes = Convert.FromBase64String(refreshToken);
        Assert.Equal(64, bytes.Length);
    }

    [Fact]
    public void ComputeRefreshTokenHash_ShouldBeDeterministicAndCaseStable()
    {
        var token = "same-input-token";

        var hashA = _service.ComputeRefreshTokenHash(token);
        var hashB = _service.ComputeRefreshTokenHash(token);
        var hashC = _service.ComputeRefreshTokenHash("other-token");

        Assert.Equal(hashA, hashB);
        Assert.NotEqual(hashA, hashC);
        Assert.Equal(64, hashA.Length);
        Assert.Matches("^[0-9A-F]+$", hashA);
    }
}
