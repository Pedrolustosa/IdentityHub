using IdentityHub.Application.Common.Results;
using IdentityHub.Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityHub.Application.CQRS.Roles.Commands
{
    public sealed record UpdateRoleCommand(string Id, UpdateRoleRequest Request) : IRequest<Result>;
}
