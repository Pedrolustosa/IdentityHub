using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.SecuritySettings.Queries;
using IdentityHub.Application.DTOs;
using IdentityHub.Application.Interfaces;
using MediatR;

namespace IdentityHub.Application.CQRS.SecuritySettings.Handlers;

public sealed class GetSecuritySettingsQueryHandler : IRequestHandler<GetSecuritySettingsQuery, Result<SecuritySettingsResponse>>
{
    private readonly ISecuritySettingsService _service;

    public GetSecuritySettingsQueryHandler(ISecuritySettingsService service)
    {
        _service = service;
    }

    public async Task<Result<SecuritySettingsResponse>> Handle(GetSecuritySettingsQuery query, CancellationToken cancellationToken)
    {
        return await _service.GetSettingsAsync(cancellationToken);
    }
}
