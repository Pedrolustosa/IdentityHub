using IdentityHub.Application.Common.Results;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityHub.Application.CQRS.RoleClaims.Commands
{
    public sealed record ReplaceRoleClaimPermissionsCommand(
        string RoleId,
        List<string> Permissions) : IRequest<Result>;
}
