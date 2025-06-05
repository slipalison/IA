namespace IA.WebApi.Controllers.Chat;

public class ChatResponse
{
    public string Message { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public List<SourceDocument> SourceDocuments { get; set; } = new();
    public ChatMetadata Metadata { get; set; } = new();
    public string? Error { get; set; }
    public bool Success => string.IsNullOrEmpty(Error);
}