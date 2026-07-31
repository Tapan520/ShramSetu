using ShramSetu.Services;
using System.Security.Claims;

namespace ShramSetu.Middleware;

public class OnlineUserMiddleware
{
    private readonly RequestDelegate _next;

    public OnlineUserMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, OnlineUserTracker tracker)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var sessionId = context.Session.Id;
            var userId    = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            var email     = context.User.FindFirstValue(ClaimTypes.Email)
                         ?? context.User.Identity.Name ?? "";
            var role      = context.User.FindFirstValue(ClaimTypes.Role)
                         ?? string.Join(", ", context.User.Claims
                                .Where(c => c.Type == ClaimTypes.Role)
                                .Select(c => c.Value));

            var existing = tracker.GetOnlineUsers().FirstOrDefault(u => u.UserId == userId);
            if (existing is not null)
            {
                existing.LastActive = DateTime.UtcNow;
                tracker.TrackUser(existing.SessionId, existing);
            }
            else
            {
                tracker.TrackUser(sessionId, new OnlineUserInfo
                {
                    SessionId  = sessionId,
                    UserId     = userId,
                    Email      = email,
                    Role       = role,
                    IpAddress  = context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    UserAgent  = context.Request.Headers["User-Agent"].ToString(),
                    LoginTime  = DateTime.UtcNow,
                    LastActive = DateTime.UtcNow
                });
            }
        }

        await _next(context);
    }
}
