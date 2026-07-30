using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Core.Enums;
using ShramSetu.Data;

namespace ShramSetu.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly ApplicationDbContext _db;

    public ChatHub(ApplicationDbContext db) => _db = db;

    /// <summary>
    /// Client calls this to send a message.
    /// The hub persists the message and broadcasts it to the recipient's user group.
    /// </summary>
    public async Task SendMessage(string recipientUserId, string body)
    {
        var senderUserId = Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // Deterministic room key so both parties share the same history
        var parts = new[] { senderUserId, recipientUserId }.OrderBy(x => x).ToArray();
        var roomKey = $"{parts[0]}_{parts[1]}";

        var msg = new ChatMessage
        {
            Id = Guid.NewGuid(),
            RoomKey = roomKey,
            SenderUserId = senderUserId,
            RecipientUserId = recipientUserId,
            Body = body,
            Status = ChatMessageStatus.Sent
        };

        _db.ChatMessages.Add(msg);
        await _db.SaveChangesAsync();

        var payload = new
        {
            id            = msg.Id,
            senderUserId,
            body,
            sentAt        = msg.SentAt
        };

        // Push to recipient's SignalR user group
        await Clients.User(recipientUserId).SendAsync("ReceiveMessage", payload);
        // Echo back to sender (for multi-device sync)
        await Clients.User(senderUserId).SendAsync("ReceiveMessage", payload);
    }

    /// <summary>Client marks a message as read.</summary>
    public async Task MarkRead(Guid messageId)
    {
        var userId = Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var msg = await _db.ChatMessages.FindAsync(messageId);
        if (msg is not null && msg.RecipientUserId == userId)
        {
            msg.Status = ChatMessageStatus.Read;
            msg.ReadAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }
}
