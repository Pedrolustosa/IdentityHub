using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityHub.Application.CQRS.RoleClaims.Commands
{
    public sealed record AddRoleClaimPermissionCommand(
        string RoleId,
        string Permission) : IRequest;
}
