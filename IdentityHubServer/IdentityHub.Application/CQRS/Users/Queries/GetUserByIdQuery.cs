using IdentityHub.Application.Common.Results;
using IdentityHub.Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityHub.Application.CQRS.Users.Queries
{
    public sealed record GetUserByIdQuery(string Id) : IRequest<Result<UserResponse>>;
}
