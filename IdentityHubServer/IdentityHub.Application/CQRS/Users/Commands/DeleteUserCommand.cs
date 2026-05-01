using IdentityHub.Application.Common.Results;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityHub.Application.CQRS.Users.Commands
{
    public sealed record DeleteUserCommand(string Id) : IRequest<Result>;
}
