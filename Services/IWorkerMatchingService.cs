using ShramSetu.Core.Models;

namespace ShramSetu.Services;

public interface IWorkerMatchingService
{
    /// <summary>Returns top N scored worker matches for a given job post.</summary>
    Task<IList<WorkerSummary>> MatchForJobAsync(Guid jobPostId, int topN = 10, CancellationToken ct = default);

    /// <summary>Returns top N scored worker matches for a sourcing request.</summary>
    Task<IList<WorkerSummary>> MatchForSourcingAsync(Guid sourcingRequestId, int topN = 10, CancellationToken ct = default);
}
