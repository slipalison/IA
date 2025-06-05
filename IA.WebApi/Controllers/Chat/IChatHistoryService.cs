namespace IA.WebApi.Controllers.Chat;

public interface IChatHistoryService
{
    Task SaveMessageAsync(string conversationId, string userMessage, string botResponse, ChatMetadata? metadata = null);
    Task<List<ChatMessage>> GetConversationHistoryAsync(string conversationId, int limit = 10);
    Task<List<ConversationSummary>> GetRecentConversationsAsync(string? userId = null, int limit = 20);
    Task DeleteConversationAsync(string conversationId);
    Task<bool> ConversationExistsAsync(string conversationId);
    Task UpdateMessageAsync(string messageId, string newContent);
    Task<ChatStatistics> GetChatStatisticsAsync(string? userId = null);
}