using ShramSetu.Core.Entities;

namespace ShramSetu.Core.Models;

/// <summary>
/// Flat projection used on the worker search page to avoid N+1 queries.
/// </summary>
public class WorkerSummary
{
    public Worker Worker { get; init; } = null!;
    public double AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public int CompletedJobCount { get; init; }
}
