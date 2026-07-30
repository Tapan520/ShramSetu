using ShramSetu.Core.Enums;

namespace ShramSetu.Core.Entities;

/// <summary>Tracks minimum required app version per platform.</summary>
public class AppVersion
{
    public Guid Id { get; set; }
    public AppPlatform Platform { get; set; }
    public string MinVersion { get; set; } = string.Empty;
    public string LatestVersion { get; set; } = string.Empty;
    public string? UpdateMessage { get; set; }
    public bool ForceUpdate { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
