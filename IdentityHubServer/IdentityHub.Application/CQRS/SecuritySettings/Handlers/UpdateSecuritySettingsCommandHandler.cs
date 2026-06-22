using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.SecuritySettings.Commands;
using IdentityHub.Application.Interfaces;
using MediatR;

namespace IdentityHub.Application.CQRS.SecuritySettings.Handlers;

public sealed class UpdateSecuritySettingsCommandHandler : IRequestHandler<UpdateSecuritySettingsCommand, Result>
{
    private readonly ISecuritySettingsService _service;

    public UpdateSecuritySettingsCommandHandler(ISecuritySettingsService service)
    {
        _service = service;
    }

    public async Task<Result> Handle(UpdateSecuritySettingsCommand command, CancellationToken cancellationToken)
    {
        return await _service.UpdateSettingsAsync(command.Request, cancellationToken);
    }
}
