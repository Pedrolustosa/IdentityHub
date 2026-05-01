using IdentityHub.Application.Common.Results;
using IdentityHub.Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityHub.Application.CQRS.Roles.Commands
{
    public sealed record CreateRoleCommand(CreateRoleRequest Request) : IRequest<Result>;
}
