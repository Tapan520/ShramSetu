using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Api.Dtos;
using ShramSetu.Core.Entities;
using ShramSetu.Data;

namespace ShramSetu.Api.Controllers;

[ApiController]
[Route("api/subscriptions")]
[Produces("application/json")]
public class SubscriptionsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public SubscriptionsController(ApplicationDbContext db) => _db = db;

    /// <summary>List all available subscription plans.</summary>
    [HttpGet("plans")]
    public async Task<ActionResult<IList<SubscriptionPlanDto>>> GetPlans()
    {
        var plans = await _db.SubscriptionPlans.Where(p => p.IsActive)
            .OrderBy(p => p.PriceMonthly).ToListAsync();
        return Ok(plans.Select(ToDto));
    }

    /// <summary>Get the authenticated employer's active subscription.</summary>
    [HttpGet("mine")]
    [Authorize(Roles = "Employer,Admin")]
    public async Task<ActionResult<EmployerSubscriptionDto>> GetMine()
    {
        var userId   = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var employer = await _db.EmployerAccounts.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employer is null) return NotFound();

        var sub = await _db.EmployerSubscriptions
            .Include(s => s.Plan)
            .Where(s => s.EmployerId == employer.Id && s.Status == Core.Enums.SubscriptionStatus.Active)
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefaultAsync();

        if (sub is null) return NotFound(new { message = "No active subscription. Default Free plan applies." });
        return Ok(ToSubDto(sub));
    }

    /// <summary>Subscribe to a plan (integrates with payment gateway  reference passed from mobile after payment).</summary>
    [HttpPost("subscribe")]
    [Authorize(Roles = "Employer,Admin")]
    public async Task<ActionResult<EmployerSubscriptionDto>> Subscribe([FromBody] SubscribeRequest req)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var userId   = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var employer = await _db.EmployerAccounts.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employer is null) return BadRequest(new { message = "Employer profile not found." });

        var plan = await _db.SubscriptionPlans.FindAsync(req.PlanId);
        if (plan is null || !plan.IsActive) return NotFound(new { message = "Plan not found." });

        // Cancel any existing active subscription
        var existing = await _db.EmployerSubscriptions
            .Where(s => s.EmployerId == employer.Id && s.Status == Core.Enums.SubscriptionStatus.Active)
            .ToListAsync();
        foreach (var old in existing) old.Status = Core.Enums.SubscriptionStatus.Cancelled;

        var durationDays = req.Yearly ? 365 : 30;
        var amount       = req.Yearly ? plan.PriceYearly : plan.PriceMonthly;

        var sub = new EmployerSubscription
        {
            Id               = Guid.NewGuid(),
            EmployerId       = employer.Id,
            PlanId           = plan.Id,
            Status           = Core.Enums.SubscriptionStatus.Active,
            StartDate        = DateTime.UtcNow,
            EndDate          = DateTime.UtcNow.AddDays(durationDays),
            PaymentReference = req.PaymentReference,
            AmountPaid       = amount
        };

        _db.EmployerSubscriptions.Add(sub);
        await _db.SaveChangesAsync();

        await _db.Entry(sub).Reference(s => s.Plan).LoadAsync();
        return CreatedAtAction(nameof(GetMine), ToSubDto(sub));
    }

    private static SubscriptionPlanDto ToDto(SubscriptionPlan p) => new(
        p.Id, p.Name, p.Tier.ToString(), p.PriceMonthly, p.PriceYearly,
        p.MaxJobPosts, p.MaxSourcingRequests, p.CanAccessChat, p.CanAccessAnalytics, p.PrioritySupport);

    private static EmployerSubscriptionDto ToSubDto(EmployerSubscription s) => new(
        s.Id, s.Plan.Name, s.Plan.Tier.ToString(), s.Status.ToString(),
        s.StartDate, s.EndDate, s.AmountPaid);
}
