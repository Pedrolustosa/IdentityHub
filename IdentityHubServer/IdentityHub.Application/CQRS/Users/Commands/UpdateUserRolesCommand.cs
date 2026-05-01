using IdentityHub.Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityHub.Application.CQRS.Users.Commands
{
    public sealed record UpdateUserRolesCommand(string Id, UpdateRolesRequest Request) : IRequest;
}
