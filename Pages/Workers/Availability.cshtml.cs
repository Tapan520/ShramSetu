using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Core.Enums;
using ShramSetu.Data;

namespace ShramSetu.Pages.Workers;

[Authorize(Roles = "Worker")]
public class AvailabilityModel : PageModel
{
    private readonly ApplicationDbContext _db;
    public AvailabilityModel(ApplicationDbContext db) => _db = db;

    public IList<WorkerAvailability> Slots { get; set; } = new List<WorkerAvailability>();
    public string CalendarEventsJson { get; set; } = "[]";

    [BindProperty]
    public SlotInput Input { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadAsync();
        if (!ModelState.IsValid) return Page();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var worker = await _db.Workers.FirstOrDefaultAsync(w => w.UserId == userId);
        if (worker is null) return NotFound();

        _db.WorkerAvailabilities.Add(new WorkerAvailability
        {
            Id = Guid.NewGuid(),
            WorkerId  = worker.Id,
            StartDate = Input.StartDate.Date,
            EndDate   = Input.EndDate.Date,
            SlotType  = Enum.Parse<AvailabilitySlotType>(Input.SlotType),
            Note      = Input.Note
        });

        await _db.SaveChangesAsync();
        TempData["Success"] = "Availability slot saved.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid slotId)
    {
        var slot = await _db.WorkerAvailabilities.FindAsync(slotId);
        if (slot is not null)
        {
            _db.WorkerAvailabilities.Remove(slot);
            await _db.SaveChangesAsync();
        }
        TempData["Success"] = "Slot removed.";
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var worker = await _db.Workers.FirstOrDefaultAsync(w => w.UserId == userId);
        if (worker is null) return;

        Slots = await _db.WorkerAvailabilities
            .Where(a => a.WorkerId == worker.Id && a.EndDate >= DateTime.Today)
            .OrderBy(a => a.StartDate)
            .ToListAsync();

        var events = Slots.Select(s => new
        {
            title = s.SlotType.ToString(),
            start = s.StartDate.ToString("yyyy-MM-dd"),
            end   = s.EndDate.AddDays(1).ToString("yyyy-MM-dd"),
            color = s.SlotType == AvailabilitySlotType.Available ? "#198754" : "#dc3545"
        });

        CalendarEventsJson = JsonSerializer.Serialize(events);
    }

    public class SlotInput
    {
        [Required, Display(Name = "Start Date"), DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Required, Display(Name = "End Date"), DataType(DataType.Date)]
        public DateTime EndDate { get; set; } = DateTime.Today;

        [Required]
        public string SlotType { get; set; } = "Available";

        [Display(Name = "Note")]
        public string? Note { get; set; }
    }
}
