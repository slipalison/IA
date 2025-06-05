namespace IA.WebApi.Controllers.Chat;

public class ChatStatistics
{
    public int TotalConversations { get; set; }
    public int TotalMessages { get; set; }
    public double AverageMessagesPerConversation { get; set; }
    public string MostActiveDay { get; set; } = string.Empty;
    public List<string> TopCategories { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}