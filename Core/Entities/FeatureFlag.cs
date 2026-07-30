using ShramSetu.Core.Enums;

namespace ShramSetu.Core.Entities;

/// <summary>Admin-configurable feature flag for runtime feature toggling.</summary>
public class FeatureFlag
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;        // e.g. "EnableJobAlerts"
    public string Description { get; set; } = string.Empty;
    public FeatureFlagStatus Status { get; set; } = FeatureFlagStatus.Enabled;
    public string UpdatedByUserId { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
