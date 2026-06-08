using IdentityHub.Application.Common.Results;
using IdentityHub.Application.DTOs;
using MediatR;

namespace IdentityHub.Application.CQRS.Auth.Queries;

public sealed record GetActiveSessionsQuery(string UserId, Guid? CurrentSessionId) : IRequest<Result<IReadOnlyList<UserSessionResponse>>>;