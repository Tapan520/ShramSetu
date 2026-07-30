using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Api.Dtos;
using ShramSetu.Core.Entities;
using ShramSetu.Core.Enums;
using ShramSetu.Data;
using ShramSetu.Services;

namespace ShramSetu.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly IJwtTokenService _jwt;
    private readonly IOtpService _otp;
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public AuthController(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        IJwtTokenService jwt,
        IOtpService otp,
        ApplicationDbContext db,
        IConfiguration config,
        IWebHostEnvironment env)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwt = jwt;
        _otp = otp;
        _db = db;
        _config = config;
        _env = env;
    }

    // ?? OTP Authentication ????????????????????????????????????????????????????

    /// <summary>
    /// Step 1  Send a 6-digit OTP via SMS to the given phone number.
    /// In Development the OTP code is returned in the response for easy testing.
    /// </summary>
    [HttpPost("otp/send")]
    public async Task<ActionResult<OtpSendResponse>> SendOtp([FromBody] OtpSendRequest req)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var code = await _otp.SendOtpAsync(req.Phone);

        return Ok(new OtpSendResponse(
            Success: true,
            Message: "OTP sent successfully.",
            DevCode: _env.IsDevelopment() ? code : null));
    }

    /// <summary>
    /// Step 2  Verify the OTP.
    ///  If the phone is already registered ? returns a JWT.
    ///  If the phone is new ? auto-creates a Worker account (FullName required) and returns a JWT.
    /// </summary>
    [HttpPost("otp/verify")]
    public async Task<ActionResult<AuthResponse>> VerifyOtp([FromBody] OtpVerifyRequest req)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var valid = await _otp.VerifyOtpAsync(req.Phone, req.Code);
        if (!valid)
            return Unauthorized(new { message = "Invalid or expired OTP." });

        // Find existing user by phone number
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == req.Phone);

        if (user is null)
        {
            // Auto-register as a Worker
            if (string.IsNullOrWhiteSpace(req.FullName))
                return BadRequest(new { message = "FullName is required for new registrations." });

            user = new IdentityUser
            {
                UserName       = req.Phone + "@worker.shramsetu.in",
                Email          = req.Phone + "@worker.shramsetu.in",
                PhoneNumber    = req.Phone,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true
            };

            // OTP-registered workers get a random password (they log in via OTP only)
            var randomPassword = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + "Aa1!";
            var result = await _userManager.CreateAsync(user, randomPassword);
            if (!result.Succeeded)
                return BadRequest(result.Errors.Select(e => e.Description));

            await _userManager.AddToRoleAsync(user, "Worker");

            // Check if a worker profile exists (registered via web without Identity account)
            var existingWorker = await _db.Workers.FirstOrDefaultAsync(w => w.Phone == req.Phone);
            if (existingWorker is null)
            {
                _db.Workers.Add(new Worker
                {
                    Id     = Guid.NewGuid(),
                    UserId = user.Id,
                    FullName = req.FullName,
                    Phone    = req.Phone,
                    // Default skill  worker should complete profile after login
                    SkillCategoryId = (await _db.SkillCategories.FirstAsync()).Id,
                    KycStatus = VerificationStatus.Pending
                });
            }
            else
            {
                existingWorker.UserId = user.Id;
            }

            await _db.SaveChangesAsync();
        }
        else
        {
            // Mark phone as confirmed if not already
            if (!user.PhoneNumberConfirmed)
            {
                user.PhoneNumberConfirmed = true;
                await _userManager.UpdateAsync(user);
            }
        }

        var roles = await _userManager.GetRolesAsync(user);
        var expiryDays = int.TryParse(_config["Jwt:ExpiryDays"], out var d) ? d : 30;
        var token = _jwt.GenerateToken(user, roles);

        return Ok(new AuthResponse(token, user.Id, user.Email!, roles, DateTime.UtcNow.AddDays(expiryDays)));
    }

    // ?? Password Authentication ???????????????????????????????????????????????

    /// <summary>Login with username/email + password and receive a JWT token.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest req)
    {
        var user = await _userManager.FindByNameAsync(req.Username)
                ?? await _userManager.FindByEmailAsync(req.Username);

        if (user is null)
            return Unauthorized(new { message = "Invalid credentials." });

        var result = await _signInManager.CheckPasswordSignInAsync(user, req.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
            return Unauthorized(new { message = "Invalid credentials." });

        var roles = await _userManager.GetRolesAsync(user);
        var expiryDays = int.TryParse(_config["Jwt:ExpiryDays"], out var d) ? d : 30;
        var token = _jwt.GenerateToken(user, roles);

        return Ok(new AuthResponse(token, user.Id, user.Email!, roles, DateTime.UtcNow.AddDays(expiryDays)));
    }

    // ?? Worker Registration ???????????????????????????????????????????????????

    /// <summary>Register a new Worker account with password.</summary>
    [HttpPost("register/worker")]
    public async Task<ActionResult<AuthResponse>> RegisterWorker([FromBody] RegisterWorkerRequest req)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var user = new IdentityUser
        {
            UserName    = req.Phone + "@worker.shramsetu.in",
            Email       = req.Phone + "@worker.shramsetu.in",
            PhoneNumber = req.Phone,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, req.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        await _userManager.AddToRoleAsync(user, "Worker");

        _db.Workers.Add(new Worker
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            FullName = req.FullName,
            Phone = req.Phone,
            SkillCategoryId = req.SkillCategoryId,
            SubSkills = req.SubSkills,
            YearsOfExperience = req.YearsOfExperience,
            ExpectedDailyWage = req.ExpectedDailyWage,
            ExpectedMonthlyWage = req.ExpectedMonthlyWage,
            LocationCity = req.LocationCity,
            LocationState = req.LocationState,
            KycStatus = VerificationStatus.Pending
        });

        await _db.SaveChangesAsync();

        var roles = new List<string> { "Worker" };
        var token = _jwt.GenerateToken(user, roles);
        var expiryDays = int.TryParse(_config["Jwt:ExpiryDays"], out var d) ? d : 30;

        return CreatedAtAction(nameof(Login),
            new AuthResponse(token, user.Id, user.Email!, roles, DateTime.UtcNow.AddDays(expiryDays)));
    }

    // ?? Employer Registration ?????????????????????????????????????????????????

    /// <summary>Register a new Employer account with password.</summary>
    [HttpPost("register/employer")]
    public async Task<ActionResult<AuthResponse>> RegisterEmployer([FromBody] RegisterEmployerRequest req)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var user = new IdentityUser
        {
            UserName    = req.Email,
            Email       = req.Email,
            PhoneNumber = req.Phone,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, req.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        await _userManager.AddToRoleAsync(user, "Employer");

        _db.EmployerAccounts.Add(new EmployerAccount
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Name = req.Name,
            Type = Enum.Parse<EmployerType>(req.Type),
            CompanyName = req.CompanyName,
            Phone = req.Phone,
            Email = req.Email
        });

        await _db.SaveChangesAsync();

        var roles = new List<string> { "Employer" };
        var token = _jwt.GenerateToken(user, roles);
        var expiryDays = int.TryParse(_config["Jwt:ExpiryDays"], out var d) ? d : 30;

        return CreatedAtAction(nameof(Login),
            new AuthResponse(token, user.Id, user.Email!, roles, DateTime.UtcNow.AddDays(expiryDays)));
    }
}
