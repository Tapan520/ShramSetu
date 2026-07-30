using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Api.Dtos;
using ShramSetu.Core.Entities;
using ShramSetu.Data;

namespace ShramSetu.Api.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
[Produces("application/json")]
public class ChatController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public ChatController(ApplicationDbContext db) => _db = db;

    /// <summary>Get message history between the authenticated user and another user.</summary>
    [HttpGet("{otherUserId}")]
    public async Task<ActionResult<PagedResult<ChatMessageDto>>> GetHistory(
        string otherUserId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        pageSize = Math.Clamp(pageSize, 1, 200);
        page     = Math.Max(1, page);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var parts  = new[] { userId, otherUserId }.OrderBy(x => x).ToArray();
        var roomKey = $"{parts[0]}_{parts[1]}";

        var total = await _db.ChatMessages.CountAsync(m => m.RoomKey == roomKey);
        var msgs  = await _db.ChatMessages
            .Where(m => m.RoomKey == roomKey)
            .OrderByDescending(m => m.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Mark delivered messages as read
        var unread = msgs.Where(m => m.RecipientUserId == userId && m.Status != Core.Enums.ChatMessageStatus.Read).ToList();
        foreach (var m in unread) { m.Status = Core.Enums.ChatMessageStatus.Read; m.ReadAt = DateTime.UtcNow; }
        if (unread.Any()) await _db.SaveChangesAsync();

        return Ok(new PagedResult<ChatMessageDto>(
            msgs.Select(m => new ChatMessageDto(m.Id, m.SenderUserId, m.RecipientUserId,
                m.Body, m.Status.ToString(), m.SentAt, m.ReadAt)).ToList(),
            page, pageSize, total));
    }

    /// <summary>Get list of conversations (distinct contacts) for the authenticated user.</summary>
    [HttpGet("conversations")]
    public async Task<ActionResult<IList<object>>> GetConversations()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var convos = await _db.ChatMessages
            .Where(m => m.SenderUserId == userId || m.RecipientUserId == userId)
            .GroupBy(m => m.RoomKey)
            .Select(g => new
            {
                RoomKey     = g.Key,
                LastMessage = g.OrderByDescending(m => m.SentAt).First().Body,
                LastAt      = g.Max(m => m.SentAt),
                Unread      = g.Count(m => m.RecipientUserId == userId && m.Status != Core.Enums.ChatMessageStatus.Read)
            })
            .OrderByDescending(c => c.LastAt)
            .ToListAsync();

        return Ok(convos);
    }
}
