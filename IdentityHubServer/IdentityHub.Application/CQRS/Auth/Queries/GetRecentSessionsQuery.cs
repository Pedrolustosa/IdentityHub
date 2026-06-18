using IdentityHub.Application.Common.Results;
using IdentityHub.Application.DTOs;
using MediatR;

namespace IdentityHub.Application.CQRS.Auth.Queries;

public sealed record GetRecentSessionsQuery(string UserId, Guid? CurrentSessionId, int Take)
    : IRequest<Result<IReadOnlyList<UserSessionResponse>>>;
