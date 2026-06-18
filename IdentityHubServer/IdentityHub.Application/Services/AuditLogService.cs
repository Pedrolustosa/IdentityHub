using IdentityHub.Application.Common.Results;
using IdentityHub.Application.Common.Errors;
using IdentityHub.Application.CQRS.AuditLogs.Queries;
using IdentityHub.Application.DTOs;
using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Interfaces;
using IdentityHub.Domain.Entities;
using MediatR;
using System.Text;

namespace IdentityHub.Application.Services;

public sealed class AuditLogService : IAuditLogService
{
    private readonly ISender _sender;
    private readonly IAuditLogRepository _repository;

    public AuditLogService(ISender sender, IAuditLogRepository repository)
    {
        _sender = sender;
        _repository = repository;
    }

    public Task<Result<PagedResponse<AuditLogItemResponse>>> GetPagedAsync(
        AuditLogFilter request,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
        => _sender.Send(new GetAuditLogsQuery(request, page, pageSize), cancellationToken);

    public async Task<Result<AuditLogItemResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entry = await _repository.GetByIdAsync(id, cancellationToken);

        if (entry is null)
            return Result<AuditLogItemResponse>.Failure(
                Error.Create("AuditLog.NotFound", "Audit log entry not found"));

        return Result<AuditLogItemResponse>.Success(new AuditLogItemResponse
        {
            Id = entry.Id,
            ActorUserId = entry.ActorUserId,
            Type = entry.Type,
            TargetId = entry.TargetId,
            Description = entry.Description,
            MetadataJson = entry.MetadataJson,
            CreatedAt = entry.CreatedAt
        });
    }

    public async Task<Result<IReadOnlyList<AuditLogItemResponse>>> GetRecentByUserAsync(
        string userId,
        int take,
        CancellationToken cancellationToken)
    {
        var entries = await _repository.GetRecentByUserAsync(userId, take, cancellationToken);

        var response = entries
            .Select(entry => new AuditLogItemResponse
            {
                Id = entry.Id,
                ActorUserId = entry.ActorUserId,
                Type = entry.Type,
                TargetId = entry.TargetId,
                Description = entry.Description,
                MetadataJson = entry.MetadataJson,
                CreatedAt = entry.CreatedAt
            })
            .ToList();

        return Result<IReadOnlyList<AuditLogItemResponse>>.Success(response);
    }

    public async Task<string> ExportCsvAsync(
        AuditLogFilter request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetAuditLogsQuery(request, 1, 10000), cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return "Id,ActorUserId,Type,TargetId,Description,MetadataJson,CreatedAt\n";
        }

        var builder = new StringBuilder();
        builder.AppendLine("Id,ActorUserId,Type,TargetId,Description,MetadataJson,CreatedAt");

        foreach (var item in result.Value.Items)
        {
            builder.Append(EscapeCsv(item.Id.ToString()));
            builder.Append(',');
            builder.Append(EscapeCsv(item.ActorUserId));
            builder.Append(',');
            builder.Append(EscapeCsv(item.Type));
            builder.Append(',');
            builder.Append(EscapeCsv(item.TargetId));
            builder.Append(',');
            builder.Append(EscapeCsv(item.Description));
            builder.Append(',');
            builder.Append(EscapeCsv(item.MetadataJson));
            builder.Append(',');
            builder.Append(EscapeCsv(item.CreatedAt.ToString("O")));
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string EscapeCsv(string? value)
    {
        var text = value ?? string.Empty;
        if (text.Contains(',') || text.Contains('"') || text.Contains('\n') || text.Contains('\r'))
        {
            return $"\"{text.Replace("\"", "\"\"")}\"";
        }

        return text;
    }
}
