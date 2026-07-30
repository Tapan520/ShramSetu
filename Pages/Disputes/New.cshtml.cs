using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ShramSetu.Core.Entities;
using ShramSetu.Core.Enums;
using ShramSetu.Data;

namespace ShramSetu.Pages.Disputes;

[Authorize]
public class NewModel : PageModel
{
    private readonly ApplicationDbContext _db;
    public NewModel(ApplicationDbContext db) => _db = db;

    [BindProperty]
    public DisputeInput Input { get; set; } = new();

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        _db.Disputes.Add(new Dispute
        {
            Id             = Guid.NewGuid(),
            RaisedByUserId = userId,
            AgainstUserId  = Input.AgainstUserId,
            Type           = Enum.Parse<DisputeType>(Input.Type),
            Title          = Input.Title,
            Description    = Input.Description
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Dispute submitted. Our team will review it within 48 hours.";
        return RedirectToPage("Index");
    }

    public class DisputeInput
    {
        [Required] public string Type { get; set; } = "NonPayment";
        [Required, MinLength(5), Display(Name = "Title")] public string Title { get; set; } = string.Empty;
        [Required, Display(Name = "Against User ID")] public string AgainstUserId { get; set; } = string.Empty;
        [Required, MinLength(20), Display(Name = "Description")] public string Description { get; set; } = string.Empty;
    }
}
