namespace IA.WebApi.Controllers.Chat;

public class ConversationSummary
{
    public string ConversationId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string FirstMessage { get; set; } = string.Empty;
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;
    public int MessageCount { get; set; }
    public string? UserId { get; set; }
    public List<string> Tags { get; set; } = new();
    public bool IsArchived { get; set; } = false;
}