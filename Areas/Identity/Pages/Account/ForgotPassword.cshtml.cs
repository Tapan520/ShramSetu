using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ShramSetu.Services;

namespace ShramSetu.Areas.Identity.Pages.Account;

public class ForgotPasswordModel : PageModel
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IEmailService _email;
    private readonly IOtpService _otp;

    public ForgotPasswordModel(UserManager<IdentityUser> userManager,
        IEmailService email, IOtpService otp)
    {
        _userManager = userManager;
        _email       = email;
        _otp         = otp;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required, Display(Name = "Email / Phone")]
        public string Email { get; set; } = string.Empty;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var user = await _userManager.FindByEmailAsync(Input.Email)
                ?? await _userManager.FindByNameAsync(Input.Email);

        if (user is not null && !string.IsNullOrEmpty(user.Email))
        {
            // Generate OTP via OtpService (uses phone if phone looks like phone, else use email as key)
            var isPhone = Input.Email.StartsWith('+') || Input.Email.All(char.IsDigit);

            if (isPhone)
            {
                // Send OTP via SMS
                var code = await _otp.SendOtpAsync(Input.Email);
                // In dev the code is returned  in production it goes via SMS
                TempData["ResetPhone"] = Input.Email;
            }
            else
            {
                // Generate a secure token and send via email
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var otp   = new Random().Next(100000, 999999).ToString();

                // Store token in TempData (in production store in DB with expiry)
                TempData["ResetToken"] = token;
                TempData["ResetEmail"] = user.Email;

                await _email.SendPasswordResetOtpAsync(user.Email, user.UserName ?? "User", otp);
            }
        }

        // Always show confirmation  never reveal if account exists
        TempData["Success"] = "If that account exists, a reset code has been sent.";
        return RedirectToPage("./ForgotPasswordConfirmation");
    }
}

