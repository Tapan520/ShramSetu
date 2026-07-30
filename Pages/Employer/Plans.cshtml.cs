using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Data;

namespace ShramSetu.Pages.Employer;

[Authorize(Roles = "Employer,Admin")]
public class PlansModel : PageModel
{
    private readonly ApplicationDbContext _db;
    public PlansModel(ApplicationDbContext db) => _db = db;

    public IList<SubscriptionPlan> Plans { get; set; } = new List<SubscriptionPlan>();
    public EmployerSubscription? ActiveSubscription { get; set; }

    public async Task OnGetAsync()
    {
        // Pull to memory first  SQLite does not support ORDER BY on decimal columns
        Plans = (await _db.SubscriptionPlans.Where(p => p.IsActive).ToListAsync())
            .OrderBy(p => p.PriceMonthly)
            .ToList();

        var userId   = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var employer = await _db.EmployerAccounts.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employer is not null)
        {
            ActiveSubscription = await _db.EmployerSubscriptions
                .Include(s => s.Plan)
                .Where(s => s.EmployerId == employer.Id
                    && s.Status == Core.Enums.SubscriptionStatus.Active
                    && s.EndDate > DateTime.UtcNow)
                .OrderByDescending(s => s.EndDate)
                .FirstOrDefaultAsync();
        }
    }

    public async Task<IActionResult> OnPostAsync(Guid planId, bool yearly)
    {
        var userId   = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var employer = await _db.EmployerAccounts.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employer is null) return RedirectToPage();

        var plan = await _db.SubscriptionPlans.FindAsync(planId);
        if (plan is null) return RedirectToPage();

        // In production: redirect to payment gateway, then call back. Here we auto-activate.
        var existing = await _db.EmployerSubscriptions
            .Where(s => s.EmployerId == employer.Id && s.Status == Core.Enums.SubscriptionStatus.Active)
            .ToListAsync();
        foreach (var old in existing) old.Status = Core.Enums.SubscriptionStatus.Cancelled;

        var days   = yearly ? 365 : 30;
        var amount = yearly ? plan.PriceYearly : plan.PriceMonthly;

        _db.EmployerSubscriptions.Add(new EmployerSubscription
        {
            Id         = Guid.NewGuid(),
            EmployerId = employer.Id,
            PlanId     = planId,
            Status     = Core.Enums.SubscriptionStatus.Active,
            StartDate  = DateTime.UtcNow,
            EndDate    = DateTime.UtcNow.AddDays(days),
            AmountPaid = amount,
            PaymentReference = "MANUAL"
        });

        await _db.SaveChangesAsync();
        TempData["Success"] = $"You are now subscribed to the {plan.Name} plan!";
        return RedirectToPage();
    }
}
