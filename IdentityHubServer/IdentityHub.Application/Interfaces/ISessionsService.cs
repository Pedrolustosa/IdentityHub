using IdentityHub.Application.Common.Results;
using IdentityHub.Application.DTOs;
using IdentityHub.Domain.Entities;

namespace IdentityHub.Application.Interfaces;

public interface ISessionsService
{
    Task<Result<PagedResponse<AdminSessionResponse>>> GetPagedAsync(
        SessionFilter filter,
        int page,
        int pageSize,
        Guid? currentSessionId,
        CancellationToken cancellationToken = default);

    Task<Result> RevokeAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
