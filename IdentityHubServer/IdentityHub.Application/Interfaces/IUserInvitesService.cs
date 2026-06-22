using IdentityHub.Application.Common.Results;
using IdentityHub.Application.DTOs;

namespace IdentityHub.Application.Interfaces;

public interface IUserInvitesService
{
    Task<Result<UserInvitesPagedResponse>> GetUserInvitesAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<Result> ResendUserInviteAsync(Guid inviteId, CancellationToken cancellationToken = default);
    Task<Result> CancelUserInviteAsync(Guid inviteId, CancellationToken cancellationToken = default);
}
