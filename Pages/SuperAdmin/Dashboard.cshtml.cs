using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Services;

namespace ShramSetu.Pages.SuperAdmin;

[Authorize(Roles = "SuperAdmin")]
public class DashboardModel : PageModel
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly OnlineUserTracker _tracker;

    public DashboardModel(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, OnlineUserTracker tracker)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _tracker     = tracker;
    }

    public int TotalUsers       { get; set; }
    public int TotalAdmins      { get; set; }
    public int TotalEmployers   { get; set; }
    public int TotalWorkers     { get; set; }
    public int OnlineNow        { get; set; }
    public IReadOnlyList<OnlineUserInfo> ActiveSessions { get; set; } = [];

    public async Task OnGetAsync()
    {
        TotalUsers     = (await _userManager.Users.CountAsync());
        TotalAdmins    = (await _userManager.GetUsersInRoleAsync("Admin")).Count;
        TotalEmployers = (await _userManager.GetUsersInRoleAsync("Employer")).Count;
        TotalWorkers   = (await _userManager.GetUsersInRoleAsync("Worker")).Count;
        ActiveSessions = _tracker.GetOnlineUsers();
        OnlineNow      = ActiveSessions.Count;
    }
}
