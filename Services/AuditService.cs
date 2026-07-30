using System.Security.Claims;
using System.Text.Json;
using ShramSetu.Core.Entities;
using ShramSetu.Core.Enums;
using ShramSetu.Data;

namespace ShramSetu.Services;

public interface IAuditService
{
    Task LogAsync(string userId, AuditAction action, string entityType, string entityId,
        object? oldValues = null, object? newValues = null, string? ipAddress = null);
}

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _db;

    public AuditService(ApplicationDbContext db) => _db = db;

    public async Task LogAsync(string userId, AuditAction action, string entityType,
        string entityId, object? oldValues = null, object? newValues = null, string? ipAddress = null)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            Id         = Guid.NewGuid(),
            UserId     = userId,
            Action     = action,
            EntityType = entityType,
            EntityId   = entityId,
            OldValues  = oldValues is null ? null : JsonSerializer.Serialize(oldValues),
            NewValues  = newValues is null ? null : JsonSerializer.Serialize(newValues),
            IpAddress  = ipAddress,
            OccurredAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }
}
