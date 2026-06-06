using IdentityHub.Application.Common.Results;
using IdentityHub.Application.DTOs;
using MediatR;

namespace IdentityHub.Application.CQRS.Auth.Queries;

public sealed record GetMeQuery(string UserId) : IRequest<Result<MeResponse>>;
