using IdentityHub.Application.Common.Results;
using IdentityHub.Application.DTOs;
using MediatR;

namespace IdentityHub.Application.CQRS.UserInvites.Queries;

public sealed record GetUserInvitesQuery(int Page = 1, int PageSize = 20) : IRequest<Result<UserInvitesPagedResponse>>;
