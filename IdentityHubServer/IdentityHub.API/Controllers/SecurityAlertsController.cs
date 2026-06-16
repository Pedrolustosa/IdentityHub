using IdentityHub.API.Extensions;
using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityHub.API.Controllers;

[ApiController]
[Route("api/security-alerts")]
[Authorize(Policy = "SecurityEvents.View")]
public sealed class SecurityAlertsController : ControllerBase
{
    private readonly ISecurityAlertsService _service;

    public SecurityAlertsController(ISecurityAlertsService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] SecurityAlertFilter request,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.GetPagedAsync(request, page, pageSize, cancellationToken);
        return result.ToActionResult();
    }
}