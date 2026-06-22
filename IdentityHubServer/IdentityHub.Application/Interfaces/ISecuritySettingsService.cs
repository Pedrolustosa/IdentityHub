using IdentityHub.Application.Common.Results;
using IdentityHub.Application.DTOs;

namespace IdentityHub.Application.Interfaces;

public interface ISecuritySettingsService
{
    Task<Result<SecuritySettingsResponse>> GetSettingsAsync(CancellationToken cancellationToken = default);
    Task<Result> UpdateSettingsAsync(UpdateSecuritySettingsRequest request, CancellationToken cancellationToken = default);
}
