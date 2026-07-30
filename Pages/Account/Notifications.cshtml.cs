using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Data;
using ShramSetu.Services;

namespace ShramSetu.Pages.Account;

[Authorize]
public class NotificationsModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly IInAppNotificationService _notifService;

    public NotificationsModel(ApplicationDbContext db, IInAppNotificationService notifService)
    {
        _db          = db;
        _notifService = notifService;
    }

    public IList<Notification> Notifications { get; set; } = new List<Notification>();
    public int UnreadCount { get; set; }

    public async Task OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        Notifications = await _db.Notifications
            .Where(n => n.UserId == userId || n.RecipientUserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .ToListAsync();

        UnreadCount = Notifications.Count(n => !n.IsRead);
    }

    public async Task<IActionResult> OnPostMarkAllReadAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await _notifService.MarkAllReadAsync(userId);
        TempData["Success"] = "All notifications marked as read.";
        return RedirectToPage();
    }
}
