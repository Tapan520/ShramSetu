using ShramSetu.Core.Enums;

namespace ShramSetu.Core.Entities;

public class SkillCategory
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? IconCssClass { get; set; }

    public ICollection<Worker> Workers { get; set; } = new List<Worker>();
    public ICollection<SourcingRequest> SourcingRequests { get; set; } = new List<SourcingRequest>();
    public ICollection<JobPost> JobPosts { get; set; } = new List<JobPost>();
}
