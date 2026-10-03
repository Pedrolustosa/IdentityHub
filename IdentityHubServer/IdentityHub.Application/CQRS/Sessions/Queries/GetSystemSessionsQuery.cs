using IdentityHub.Application.Common.Results;
using IdentityHub.Application.DTOs;
using IdentityHub.Domain.Entities;
using MediatR;

namespace IdentityHub.Application.CQRS.Sessions.Queries;

public sealed record GetSystemSessionsQuery(
    SessionFilter Filter,
    int Page,
    int PageSize,
    Guid? CurrentSessionId) : IRequest<Result<PagedResponse<AdminSessionResponse>>>;
