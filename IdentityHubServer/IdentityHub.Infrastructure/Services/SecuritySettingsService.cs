using IdentityHub.Application.Common.Results;
using IdentityHub.Application.DTOs;
using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Entities;
using IdentityHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IdentityHub.Infrastructure.Services;

public sealed class SecuritySettingsService : ISecuritySettingsService
{
    private readonly AppDbContext _dbContext;

    public SecuritySettingsService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<SecuritySettingsResponse>> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        // Get the first (and typically only) security settings record, or return defaults
        var settings = await _dbContext.SecuritySettings
            .FirstOrDefaultAsync(cancellationToken);

        if (settings is null)
        {
            // Return default settings if none exist in database
            var response = new SecuritySettingsResponse
            {
                AccessTokenMinutes = 30,
                RefreshTokenDays = 7,
                MaxLoginAttempts = 5,
                LockDurationMinutes = 15,
                RequireEmailConfirmation = true
            };

            return Result<SecuritySettingsResponse>.Success(response);
        }

        var result = new SecuritySettingsResponse
        {
            AccessTokenMinutes = settings.AccessTokenMinutes,
            RefreshTokenDays = settings.RefreshTokenDays,
            MaxLoginAttempts = settings.MaxLoginAttempts,
            LockDurationMinutes = settings.LockDurationMinutes,
            RequireEmailConfirmation = settings.RequireEmailConfirmation
        };

        return Result<SecuritySettingsResponse>.Success(result);
    }

    public async Task<Result> UpdateSettingsAsync(UpdateSecuritySettingsRequest request, CancellationToken cancellationToken = default)
    {
        var settings = await _dbContext.SecuritySettings
            .FirstOrDefaultAsync(cancellationToken);

        if (settings is null)
        {
            // Create new security settings if none exist
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
            // Update existing security settings
            settings.AccessTokenMinutes = request.AccessTokenMinutes;
            settings.RefreshTokenDays = request.RefreshTokenDays;
            settings.MaxLoginAttempts = request.MaxLoginAttempts;
            settings.LockDurationMinutes = request.LockDurationMinutes;
            settings.RequireEmailConfirmation = request.RequireEmailConfirmation;
            settings.UpdatedAt = DateTime.UtcNow;

            _dbContext.SecuritySettings.Update(settings);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
