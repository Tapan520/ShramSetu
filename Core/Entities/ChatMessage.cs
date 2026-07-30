using ShramSetu.Core.Enums;

namespace ShramSetu.Core.Entities;

/// <summary>In-app chat message between two Identity users (worker ? employer).</summary>
public class ChatMessage
{
    public Guid Id { get; set; }

    /// <summary>Composite room key: sorted pair of UserId strings joined by '_'.</summary>
    public string RoomKey { get; set; } = string.Empty;

    public string SenderUserId { get; set; } = string.Empty;
    public string RecipientUserId { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;
    public ChatMessageStatus Status { get; set; } = ChatMessageStatus.Sent;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
}
