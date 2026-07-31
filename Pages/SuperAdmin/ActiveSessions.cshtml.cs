using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ShramSetu.Services;

namespace ShramSetu.Pages.SuperAdmin;

[Authorize(Roles = "SuperAdmin")]
public class ActiveSessionsModel : PageModel
{
    private readonly OnlineUserTracker _tracker;
    public ActiveSessionsModel(OnlineUserTracker tracker) => _tracker = tracker;

    public IReadOnlyList<OnlineUserInfo> Sessions { get; set; } = [];

    public void OnGet() => Sessions = _tracker.GetOnlineUsers();
}
