using IdentityHub.API.Extensions;
using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityHub.API.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Policy = "Audit.View")]
public sealed class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _service;

    public AuditLogsController(IAuditLogService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] AuditLogFilter request,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.GetPagedAsync(request, page, pageSize, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportCsv(
        [FromQuery] AuditLogFilter request,
        CancellationToken cancellationToken = default)
    {
        var csv = await _service.ExportCsvAsync(request, cancellationToken);
        var bytes = System.Text.Encoding.UTF8.GetBytes(csv);

        return File(bytes, "text/csv", $"audit-logs-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }
}
