using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Core.Enums;
using ShramSetu.Data;
using ShramSetu.Services;

namespace ShramSetu.Pages.Account;

[Authorize(Roles = "Employer,Admin")]
public class BookModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notify;

    public BookModel(ApplicationDbContext db, INotificationService notify)
    {
        _db = db;
        _notify = notify;
    }

    public Worker? Worker { get; set; }

    [BindProperty]
    public BookingInputModel Input { get; set; } = new();

    public async Task OnGetAsync(Guid workerId)
    {
        Worker = await LoadWorkerAsync(workerId);
        Input.WorkerId = workerId;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            Worker = await LoadWorkerAsync(Input.WorkerId);
            return Page();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var employer = await _db.EmployerAccounts.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employer is null)
        {
            ModelState.AddModelError(string.Empty, "No employer profile found. Please complete registration.");
            Worker = await LoadWorkerAsync(Input.WorkerId);
            return Page();
        }

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            WorkerId = Input.WorkerId,
            EmployerId = employer.Id,
            Type = BookingType.DirectContact,
            Status = BookingStatus.Requested,
            StartDate = Input.StartDate,
            DurationDays = Input.DurationDays,
            AgreedWage = Input.AgreedWage,
            Notes = Input.Notes
        };

        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();

        // Notify worker
        var worker = await _db.Workers.FindAsync(Input.WorkerId);
        if (worker?.UserId is not null)
        {
            await _notify.SendAsync(
                worker.UserId,
                $"New booking request from {employer.Name} starting {Input.StartDate:dd MMM yyyy} for {Input.DurationDays} day(s).",
                NotificationChannel.SMS);
        }

        TempData["Success"] = "Booking request sent! The worker will be notified.";
        return RedirectToPage("/Account/Bookings");
    }

    private Task<Worker?> LoadWorkerAsync(Guid workerId) =>
        _db.Workers.Include(w => w.SkillCategory).FirstOrDefaultAsync(w => w.Id == workerId);

    public class BookingInputModel
    {
        [Required]
        public Guid WorkerId { get; set; }

        [Required, Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Today.AddDays(1);

        [Required, Range(1, 3650), Display(Name = "Duration (days)")]
        public int DurationDays { get; set; } = 1;

        [Required, Range(1, 100000), Display(Name = "Agreed Daily Wage (?)")]
        public decimal AgreedWage { get; set; }

        [Display(Name = "Notes / Instructions")]
        public string? Notes { get; set; }
    }
}
