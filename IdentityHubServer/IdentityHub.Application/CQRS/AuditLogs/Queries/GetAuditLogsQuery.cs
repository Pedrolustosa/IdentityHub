using IdentityHub.Application.Common.Results;
using IdentityHub.Application.DTOs;
using IdentityHub.Domain.Entities;
using MediatR;

namespace IdentityHub.Application.CQRS.AuditLogs.Queries;

public sealed record GetAuditLogsQuery(AuditLogFilter Request, int Page, int PageSize)
    : IRequest<Result<PagedResponse<AuditLogItemResponse>>>;
