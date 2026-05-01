using IdentityHub.Application.Common.Results;
using IdentityHub.Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityHub.Application.CQRS.Users.Commands
{
    public sealed record UpdateUserCommand(string Id, UpdateUserRequest Request) : IRequest<Result>;
}
