using IdentityHub.Application.Common.Results;
using IdentityHub.Application.Common.Security;
using IdentityHub.Application.DTOs;
using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Entities;
using IdentityHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace IdentityHub.Infrastructure.Services;

public sealed class SecuritySettingsService : ISecuritySettingsService
{
    public const string CacheKey = "identityhub:security-settings";

    private readonly AppDbContext _dbContext;
    private readonly IMemoryCache _cache;

    public SecuritySettingsService(AppDbContext dbContext, IMemoryCache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public async Task<Result<SecuritySettingsResponse>> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKey, out SecuritySettingsResponse? cached) && cached is not null)
            return Result<SecuritySettingsResponse>.Success(cached);

        var settings = await _dbContext.SecuritySettings
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        var response = settings is null
            ? SecuritySettingsDefaults.Create()
            : new SecuritySettingsResponse
            {
                AccessTokenMinutes = settings.AccessTokenMinutes,
                RefreshTokenDays = settings.RefreshTokenDays,
                MaxLoginAttempts = settings.MaxLoginAttempts,
                LockDurationMinutes = settings.LockDurationMinutes,
                RequireEmailConfirmation = settings.RequireEmailConfirmation
            };

        _cache.Set(CacheKey, response, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        });

        return Result<SecuritySettingsResponse>.Success(response);
    }

    public async Task<Result> UpdateSettingsAsync(UpdateSecuritySettingsRequest request, CancellationToken cancellationToken = default)
    {
        var settings = await _dbContext.SecuritySettings
            .FirstOrDefaultAsync(cancellationToken);

        if (settings is null)
        {
            settings = new SecuritySetting
            {
                Id = Guid.NewGuid(),
                AccessTokenMinutes = request.AccessTokenMinutes,
                RefreshTokenDays = request.RefreshTokenDays,
                MaxLoginAttempts = request.MaxLoginAttempts,
                LockDurationMinutes = request.LockDurationMinutes,
                RequireEmailConfirmation = request.RequireEmailConfirmation,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.SecuritySettings.Add(settings);
        }
        else
        {
            settings.AccessTokenMinutes = request.AccessTokenMinutes;
            settings.RefreshTokenDays = request.RefreshTokenDays;
            settings.MaxLoginAttempts = request.MaxLoginAttempts;
            settings.LockDurationMinutes = request.LockDurationMinutes;
            settings.RequireEmailConfirmation = request.RequireEmailConfirmation;
            settings.UpdatedAt = DateTime.UtcNow;

            _dbContext.SecuritySettings.Update(settings);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _cache.Remove(CacheKey);

        return Result.Success();
    }
}
