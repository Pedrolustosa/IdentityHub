using IdentityHub.Application.Common.Results;
using IdentityHub.Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityHub.Application.CQRS.Roles.Queries
{
    public sealed record GetRolesQuery : IRequest<Result<List<RoleResponse>>>;
}
