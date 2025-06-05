namespace IA.WebApi.Controllers.Chat;

public class ChatMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ConversationId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsFromUser { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public List<SourceDocument> Sources { get; set; } = new();
    public ChatMetadata? Metadata { get; set; }
    public string? UserId { get; set; }
    public MessageStatus Status { get; set; } = MessageStatus.Delivered;
}