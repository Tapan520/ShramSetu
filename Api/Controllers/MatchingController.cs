using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Api.Dtos;
using ShramSetu.Core.Models;
using ShramSetu.Data;
using ShramSetu.Services;

namespace ShramSetu.Api.Controllers;

[ApiController]
[Route("api/match")]
[Authorize(Roles = "Employer,Admin")]
[Produces("application/json")]
public class MatchingController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IWorkerMatchingService _matcher;

    public MatchingController(ApplicationDbContext db, IWorkerMatchingService matcher)
    {
        _db = db;
        _matcher = matcher;
    }

    /// <summary>Get AI-ranked worker matches for a specific job post.</summary>
    [HttpGet("job/{jobPostId:guid}")]
    public async Task<ActionResult<IList<MatchedWorkerDto>>> MatchForJob(
        Guid jobPostId, [FromQuery] int topN = 10)
    {
        var matches = await _matcher.MatchForJobAsync(jobPostId, topN);
        return Ok(ToMatchDtos(matches, jobPostId, null));
    }

    /// <summary>Get AI-ranked worker matches for a sourcing request.</summary>
    [HttpGet("sourcing/{sourcingRequestId:guid}")]
    public async Task<ActionResult<IList<MatchedWorkerDto>>> MatchForSourcing(
        Guid sourcingRequestId, [FromQuery] int topN = 10)
    {
        var matches = await _matcher.MatchForSourcingAsync(sourcingRequestId, topN);
        return Ok(ToMatchDtos(matches, null, sourcingRequestId));
    }

    private async Task<IList<MatchedWorkerDto>> ToMatchDtos(
        IList<WorkerSummary> summaries, Guid? jobPostId, Guid? sourcingRequestId)
    {
        var workerIds = summaries.Select(s => s.Worker.Id).ToList();
        var scores = await _db.WorkerMatchScores
            .Where(s => workerIds.Contains(s.WorkerId)
                && s.JobPostId == jobPostId
                && s.SourcingRequestId == sourcingRequestId)
            .ToDictionaryAsync(s => s.WorkerId, s => (s.Score, s.Reason));

        return summaries.Select(s =>
        {
            scores.TryGetValue(s.Worker.Id, out var sc);
            return new MatchedWorkerDto(
                new WorkerCardDto(
                    s.Worker.Id, s.Worker.FullName, s.Worker.SkillCategory.Name,
                    s.Worker.LocationCity, s.Worker.LocationState,
                    s.Worker.YearsOfExperience, s.Worker.ExpectedDailyWage,
                    s.Worker.KycStatus.ToString(), s.Worker.IsAvailable, s.Worker.PhotoUrl,
                    s.AverageRating, s.ReviewCount, s.CompletedJobCount),
                sc.Score,
                sc.Reason ?? string.Empty);
        }).ToList();
    }
}
