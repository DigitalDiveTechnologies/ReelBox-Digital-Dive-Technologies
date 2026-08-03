using System.Text.Json;
using SocialReelSaver.Application.Abstractions.Admin;
using SocialReelSaver.Domain.Entities;

namespace SocialReelSaver.Infrastructure.Admin;

public sealed class AuditLogWriter : IAuditLogWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IAuditLogRepository _logs;

    public AuditLogWriter(IAuditLogRepository logs)
    {
        _logs = logs;
    }

    public async Task WriteAsync(
        Guid adminId,
        string adminEmail,
        string action,
        string entityType,
        string? entityId,
        object? oldValues,
        object? newValues,
        string? ipAddress,
        string? correlationId,
        CancellationToken cancellationToken = default)
    {
        var entry = new AuditLog
        {
            Id = Guid.NewGuid(),
            AdminId = adminId,
            AdminEmail = adminEmail,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValuesJson = oldValues is null ? null : JsonSerializer.Serialize(oldValues, JsonOptions),
            NewValuesJson = newValues is null ? null : JsonSerializer.Serialize(newValues, JsonOptions),
            IpAddress = ipAddress,
            CorrelationId = correlationId,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await _logs.AddAsync(entry, cancellationToken);
        await _logs.SaveChangesAsync(cancellationToken);
    }
}
