using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Data;

namespace ShramSetu.Pages.Admin;

[Authorize(Roles = "Admin,SuperAdmin")]
public class AuditLogsModel : PageModel
{
    private readonly ApplicationDbContext _db;
    public AuditLogsModel(ApplicationDbContext db) => _db = db;

    public IList<AuditLog> Logs { get; set; } = new List<AuditLog>();
    public string? EntityType { get; set; }

    public async Task OnGetAsync(string? entityType, int page = 1)
    {
        EntityType = entityType;
        var query  = _db.AuditLogs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(a => a.EntityType == entityType);

        Logs = await query
            .OrderByDescending(a => a.OccurredAt)
            .Take(200)
            .ToListAsync();
    }
}
