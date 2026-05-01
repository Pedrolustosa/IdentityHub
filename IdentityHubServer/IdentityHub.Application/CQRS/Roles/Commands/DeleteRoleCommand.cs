using IdentityHub.Application.Common.Results;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityHub.Application.CQRS.Roles.Commands
{
    public sealed record DeleteRoleCommand(string Id) : IRequest<Result>;
}
