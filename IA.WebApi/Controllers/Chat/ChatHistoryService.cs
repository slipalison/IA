using System.Collections.Concurrent;

namespace IA.WebApi.Controllers.Chat;

public class ChatHistoryService : IChatHistoryService
{
    // 🔄 CACHE EM MEMÓRIA (TEMPORÁRIO - substituir por Redis/PostgreSQL)
    private readonly ConcurrentDictionary<string, List<ChatMessage>> _conversationCache = new();
    private readonly ConcurrentDictionary<string, ConversationSummary> _conversationSummaries = new();
    private readonly ILogger<ChatHistoryService> _logger;

    public ChatHistoryService(ILogger<ChatHistoryService> logger)
    {
        _logger = logger;
    }

    public async Task SaveMessageAsync(string conversationId, string userMessage, string botResponse,
        ChatMetadata? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
            throw new ArgumentException("ID da conversa não pode estar vazio", nameof(conversationId));

        try
        {
            var messages = GetOrCreateConversation(conversationId);

            var userMsg = CreateUserMessage(conversationId, userMessage);
            var botMsg = CreateBotMessage(conversationId, botResponse, metadata);

            messages.Add(userMsg);
            messages.Add(botMsg);

            UpdateConversationSummary(conversationId, userMessage);

            _logger.LogInformation("Mensagens salvas na conversa {ConversationId}", conversationId);
            await Task.CompletedTask; // Para manter interface async
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao salvar mensagem na conversa {ConversationId}", conversationId);
            throw;
        }
    }

    public async Task<List<ChatMessage>> GetConversationHistoryAsync(string conversationId, int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
            return new List<ChatMessage>();

        try
        {
            if (!_conversationCache.TryGetValue(conversationId, out var messages))
            {
                _logger.LogInformation("Conversa {ConversationId} não encontrada", conversationId);
                return new List<ChatMessage>();
            }

            var orderedMessages = messages
                .OrderBy(m => m.Timestamp)
                .TakeLast(limit * 2) // Usuário + Bot = 2 mensagens por interação
                .ToList();

            _logger.LogInformation("Recuperadas {Count} mensagens da conversa {ConversationId}",
                orderedMessages.Count, conversationId);

            return await Task.FromResult(orderedMessages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao recuperar histórico da conversa {ConversationId}", conversationId);
            return new List<ChatMessage>();
        }
    }

    public async Task<List<ConversationSummary>> GetRecentConversationsAsync(string? userId = null, int limit = 20)
    {
        try
        {
            var summaries = _conversationSummaries.Values
                .Where(s => string.IsNullOrEmpty(userId) || s.UserId == userId)
                .OrderByDescending(s => s.LastMessageAt)
                .Take(limit)
                .ToList();

            _logger.LogInformation("Recuperadas {Count} conversas recentes", summaries.Count);
            return await Task.FromResult(summaries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao recuperar conversas recentes");
            return new List<ConversationSummary>();
        }
    }

    public async Task DeleteConversationAsync(string conversationId)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
            throw new ArgumentException("ID da conversa não pode estar vazio", nameof(conversationId));

        try
        {
            _conversationCache.TryRemove(conversationId, out _);
            _conversationSummaries.TryRemove(conversationId, out _);

            _logger.LogInformation("Conversa {ConversationId} deletada", conversationId);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar conversa {ConversationId}", conversationId);
            throw;
        }
    }

    public async Task<bool> ConversationExistsAsync(string conversationId)
    {
        var exists = !string.IsNullOrWhiteSpace(conversationId) &&
                     _conversationCache.ContainsKey(conversationId);

        return await Task.FromResult(exists);
    }

    public async Task UpdateMessageAsync(string messageId, string newContent)
    {
        if (string.IsNullOrWhiteSpace(messageId))
            throw new ArgumentException("ID da mensagem não pode estar vazio", nameof(messageId));

        try
        {
            var message = FindMessageById(messageId);
            if (message != null)
            {
                message.Message = newContent;
                message.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation("Mensagem {MessageId} atualizada", messageId);
            }
            else
            {
                _logger.LogWarning("Mensagem {MessageId} não encontrada para atualização", messageId);
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar mensagem {MessageId}", messageId);
            throw;
        }
    }

    public async Task<ChatStatistics> GetChatStatisticsAsync(string? userId = null)
    {
        try
        {
            var relevantConversations = string.IsNullOrEmpty(userId)
                ? _conversationSummaries.Values
                : _conversationSummaries.Values.Where(s => s.UserId == userId);

            var totalConversations = relevantConversations.Count();
            var totalMessages = _conversationCache.Values.Sum(messages => messages.Count);
            var avgMessagesPerConversation = totalConversations > 0 ? (double)totalMessages / totalConversations : 0;

            var stats = new ChatStatistics
            {
                TotalConversations = totalConversations,
                TotalMessages = totalMessages,
                AverageMessagesPerConversation = Math.Round(avgMessagesPerConversation, 2),
                MostActiveDay = GetMostActiveDay(),
                TopCategories = GetTopCategories()
            };

            return await Task.FromResult(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao calcular estatísticas do chat");
            return new ChatStatistics();
        }
    }

    // 🔧 MÉTODOS PRIVADOS AUXILIARES

    private List<ChatMessage> GetOrCreateConversation(string conversationId)
    {
        return _conversationCache.GetOrAdd(conversationId, _ => new List<ChatMessage>());
    }

    private static ChatMessage CreateUserMessage(string conversationId, string message)
    {
        return new ChatMessage
        {
            Id = Guid.NewGuid().ToString(),
            ConversationId = conversationId,
            Message = message,
            IsFromUser = true,
            Timestamp = DateTime.UtcNow,
            Sources = new List<SourceDocument>()
        };
    }

    private static ChatMessage CreateBotMessage(string conversationId, string message, ChatMetadata? metadata)
    {
        return new ChatMessage
        {
            Id = Guid.NewGuid().ToString(),
            ConversationId = conversationId,
            Message = message,
            IsFromUser = false,
            Timestamp = DateTime.UtcNow,
            Sources = new List<SourceDocument>(),
            Metadata = metadata
        };
    }

    private void UpdateConversationSummary(string conversationId, string lastUserMessage)
    {
        _conversationSummaries.AddOrUpdate(conversationId,
            _ => CreateNewConversationSummary(conversationId, lastUserMessage),
            (_, existing) => UpdateExistingConversationSummary(existing, lastUserMessage));
    }

    private static ConversationSummary CreateNewConversationSummary(string conversationId, string firstMessage)
    {
        return new ConversationSummary
        {
            ConversationId = conversationId,
            Title = GenerateConversationTitle(firstMessage),
            FirstMessage = firstMessage,
            LastMessageAt = DateTime.UtcNow,
            MessageCount = 1,
            UserId = "anonymous" // TODO: Implementar autenticação
        };
    }

    private static ConversationSummary UpdateExistingConversationSummary(ConversationSummary existing,
        string newMessage)
    {
        existing.LastMessageAt = DateTime.UtcNow;
        existing.MessageCount++;
        return existing;
    }

    private static string GenerateConversationTitle(string firstMessage)
    {
        const int maxTitleLength = 50;

        if (string.IsNullOrWhiteSpace(firstMessage))
            return "Nova Conversa";

        var title = firstMessage.Length <= maxTitleLength
            ? firstMessage
            : firstMessage.Substring(0, maxTitleLength) + "...";

        return title.Trim();
    }

    private ChatMessage? FindMessageById(string messageId)
    {
        return _conversationCache.Values
            .SelectMany(messages => messages)
            .FirstOrDefault(m => m.Id == messageId);
    }

    private string GetMostActiveDay()
    {
        var messageCounts = _conversationCache.Values
            .SelectMany(messages => messages)
            .GroupBy(m => m.Timestamp.DayOfWeek)
            .ToDictionary(g => g.Key, g => g.Count());

        return messageCounts.Any()
            ? messageCounts.OrderByDescending(kvp => kvp.Value).First().Key.ToString()
            : "Nenhum";
    }

    private List<string> GetTopCategories()
    {
        // TODO: Implementar análise de categorias baseada no conteúdo das mensagens
        return new List<string> { "DevOps", "Docker", "Kubernetes", "CI/CD", "Monitoramento" };
    }
}