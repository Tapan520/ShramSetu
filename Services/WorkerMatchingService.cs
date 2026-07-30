using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Core.Enums;
using ShramSetu.Core.Models;
using ShramSetu.Data;

namespace ShramSetu.Services;

/// <summary>
/// Rule-based AI worker matching service.
/// Scores workers 0100 against a job's requirements.
/// Weights:
///   Skill match        : 40 pts
///   Location match     : 20 pts
///   Wage fit           : 15 pts
///   Experience         : 10 pts
///   KYC verified       : 10 pts
///   Rating             :  5 pts
/// Persists scores to WorkerMatchScores for transparency.
/// </summary>
public class WorkerMatchingService : IWorkerMatchingService
{
    private readonly ApplicationDbContext _db;

    public WorkerMatchingService(ApplicationDbContext db) => _db = db;

    public async Task<IList<WorkerSummary>> MatchForJobAsync(Guid jobPostId, int topN = 10, CancellationToken ct = default)
    {
        var job = await _db.JobPosts
            .Include(j => j.SkillCategory)
            .FirstOrDefaultAsync(j => j.Id == jobPostId, ct);

        if (job is null) return [];

        return await ScoreAndPersistAsync(
            skillCategoryId: job.SkillCategoryId,
            city: job.LocationCity,
            budgetPerDay: job.DailyWage,
            jobPostId: jobPostId,
            sourcingRequestId: null,
            topN, ct);
    }

    public async Task<IList<WorkerSummary>> MatchForSourcingAsync(Guid sourcingRequestId, int topN = 10, CancellationToken ct = default)
    {
        var req = await _db.SourcingRequests
            .FirstOrDefaultAsync(r => r.Id == sourcingRequestId, ct);

        if (req is null) return [];

        return await ScoreAndPersistAsync(
            skillCategoryId: req.SkillCategoryId,
            city: req.LocationCity,
            budgetPerDay: req.BudgetPerDay,
            jobPostId: null,
            sourcingRequestId: sourcingRequestId,
            topN, ct);
    }

    private async Task<IList<WorkerSummary>> ScoreAndPersistAsync(
        Guid skillCategoryId, string? city, decimal budgetPerDay,
        Guid? jobPostId, Guid? sourcingRequestId,
        int topN, CancellationToken ct)
    {
        var workers = await _db.Workers
            .Include(w => w.SkillCategory)
            .Where(w => w.IsAvailable)
            .ToListAsync(ct);

        var workerIds = workers.Select(w => w.Id).ToList();

        var reviewStats = await _db.Reviews
            .Where(r => workerIds.Contains(r.WorkerId))
            .GroupBy(r => r.WorkerId)
            .Select(g => new { WorkerId = g.Key, Avg = g.Average(r => r.Rating), Count = g.Count() })
            .ToListAsync(ct);

        var completedCounts = await _db.Bookings
            .Where(b => workerIds.Contains(b.WorkerId) && b.Status == BookingStatus.Completed)
            .GroupBy(b => b.WorkerId)
            .Select(g => new { WorkerId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var reviewDict    = reviewStats.ToDictionary(x => x.WorkerId, x => (x.Avg, x.Count));
        var completedDict = completedCounts.ToDictionary(x => x.WorkerId, x => x.Count);

        var scored = workers.Select(w =>
        {
            reviewDict.TryGetValue(w.Id, out var rv);
            completedDict.TryGetValue(w.Id, out var cc);

            double score = 0;
            var reasons  = new List<string>();

            // 40  skill match
            if (w.SkillCategoryId == skillCategoryId)
            { score += 40; reasons.Add("Skill match"); }

            // 20  location
            if (!string.IsNullOrWhiteSpace(city) &&
                string.Equals(w.LocationCity, city, StringComparison.OrdinalIgnoreCase))
            { score += 20; reasons.Add("Same city"); }
            else if (!string.IsNullOrWhiteSpace(city))
            { score += 5; reasons.Add("Different city"); }

            // 15  wage fit (worker expects ? budget)
            if (w.ExpectedDailyWage <= budgetPerDay)
            {
                var fitRatio = (double)(budgetPerDay - w.ExpectedDailyWage) / (double)budgetPerDay;
                score += 15 * Math.Min(1.0, 1 - fitRatio * 0.5);
                reasons.Add("Wage within budget");
            }

            // 10  experience (capped at 10 yrs = full points)
            score += Math.Min(10, w.YearsOfExperience);
            if (w.YearsOfExperience > 0) reasons.Add($"{w.YearsOfExperience} yr exp");

            // 10  KYC
            if (w.KycStatus == VerificationStatus.Verified)
            { score += 10; reasons.Add("KYC verified"); }

            // 5  rating
            if (rv.Avg > 0) score += rv.Avg;   // max 5

            return new
            {
                Worker = w,
                Score = Math.Round(score, 1),
                Reason = string.Join(", ", reasons),
                AverageRating = rv.Avg,
                ReviewCount = rv.Count,
                CompletedJobCount = cc
            };
        })
        .OrderByDescending(x => x.Score)
        .Take(topN)
        .ToList();

        // Persist scores (upsert-style: delete old then insert)
        var oldScores = await _db.WorkerMatchScores
            .Where(s => s.JobPostId == jobPostId && s.SourcingRequestId == sourcingRequestId)
            .ToListAsync(ct);
        _db.WorkerMatchScores.RemoveRange(oldScores);

        _db.WorkerMatchScores.AddRange(scored.Select(s => new WorkerMatchScore
        {
            Id = Guid.NewGuid(),
            WorkerId = s.Worker.Id,
            JobPostId = jobPostId,
            SourcingRequestId = sourcingRequestId,
            Score = s.Score,
            Reason = s.Reason
        }));
        await _db.SaveChangesAsync(ct);

        return scored.Select(s => new WorkerSummary
        {
            Worker = s.Worker,
            AverageRating = s.AverageRating,
            ReviewCount = s.ReviewCount,
            CompletedJobCount = s.CompletedJobCount
        }).ToList();
    }
}
