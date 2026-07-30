using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ShramSetu.Areas.Identity.Pages.Account;

public class RegisterModel : PageModel
{
    public IActionResult OnGet()
    {
        // Redirect to our custom worker registration page by default.
        // The page itself offers both Worker and Employer options.
        return Page();
    }
}
