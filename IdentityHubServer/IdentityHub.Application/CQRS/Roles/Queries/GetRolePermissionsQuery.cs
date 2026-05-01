using IdentityHub.Application.Common.Results;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityHub.Application.CQRS.Roles.Queries
{
    public sealed record GetRolePermissionsQuery(string RoleId) : IRequest<Result<List<string>>>;
}
