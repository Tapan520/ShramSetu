using System.Collections.Concurrent;

namespace ShramSetu.Services;

/// <summary>
/// Singleton service that tracks currently logged-in user sessions.
/// </summary>
public class OnlineUserTracker
{
    private readonly ConcurrentDictionary<string, OnlineUserInfo> _sessions = new();

    public void TrackUser(string sessionId, OnlineUserInfo info)
        => _sessions[sessionId] = info;

    public void RemoveUser(string sessionId)
        => _sessions.TryRemove(sessionId, out _);

    public IReadOnlyList<OnlineUserInfo> GetOnlineUsers()
        => _sessions.Values.OrderByDescending(u => u.LastActive).ToList();

    public int OnlineCount => _sessions.Count;
}

public class OnlineUserInfo
{
    public string SessionId   { get; set; } = string.Empty;
    public string UserId      { get; set; } = string.Empty;
    public string Email       { get; set; } = string.Empty;
    public string Role        { get; set; } = string.Empty;
    public string IpAddress   { get; set; } = string.Empty;
    public string UserAgent   { get; set; } = string.Empty;
    public DateTime LoginTime { get; set; } = DateTime.UtcNow;
    public DateTime LastActive{ get; set; } = DateTime.UtcNow;
}
