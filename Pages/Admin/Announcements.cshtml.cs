using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Core.Enums;
using ShramSetu.Data;

namespace ShramSetu.Pages.Admin;

[Authorize(Roles = "Admin,SuperAdmin")]
public class AnnouncementsModel : PageModel
{
    private readonly ApplicationDbContext _db;
    public AnnouncementsModel(ApplicationDbContext db) => _db = db;

    public IList<Announcement> Announcements { get; set; } = new List<Announcement>();

    [BindProperty]
    public AnnouncementInput Input { get; set; } = new();

    public async Task OnGetAsync()
        => Announcements = await _db.Announcements.Where(a => a.IsActive).OrderByDescending(a => a.CreatedAt).ToListAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) { await OnGetAsync(); return Page(); }
        _db.Announcements.Add(new Announcement
        {
            Id = Guid.NewGuid(), Title = Input.Title, Body = Input.Body,
            Target = Enum.Parse<AnnouncementTarget>(Input.Target),
            SendPush = Input.SendPush, SendSms = Input.SendSms, ShowBanner = Input.ShowBanner,
            ExpiresAt = Input.ExpiresAt,
            CreatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Announcement published.";
        return RedirectToPage();
    }

    public class AnnouncementInput
    {
        [Required] public string Title { get; set; } = string.Empty;
        [Required] public string Body { get; set; } = string.Empty;
        public string Target { get; set; } = "All";
        public bool ShowBanner { get; set; } = true;
        public bool SendPush { get; set; } = true;
        public bool SendSms { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
