using IA.WebApi.Services;

namespace IA.WebApi.Controllers.Chat;

public class ChatService : IChatService
{
    private readonly IRelevantDocumentRetriever _documentRetriever;
    private readonly IChatHistoryService _historyService;
    private readonly ILogger<ChatService> _logger;
    private readonly IContextualResponseGenerator _responseGenerator;

    public ChatService(
        IContextualResponseGenerator responseGenerator,
        IRelevantDocumentRetriever documentRetriever,
        IChatHistoryService historyService,
        ILogger<ChatService> logger)
    {
        _responseGenerator = responseGenerator;
        _documentRetriever = documentRetriever;
        _historyService = historyService;
        _logger = logger;
    }

    public async Task<ChatResponse> ProcessMessageAsync(ChatRequest request)
    {
        ValidateRequest(request);

        var conversationId = EnsureConversationId(request.ConversationId);
        var relevantDocuments = await RetrieveRelevantDocuments(request);
        var botMessage = await GenerateResponse(request.Message, relevantDocuments);

        await SaveConversationHistory(conversationId, request.Message, botMessage);

        return CreateChatResponse(conversationId, botMessage, relevantDocuments);
    }

    public async Task StreamMessageAsync(ChatRequest request, HttpResponse response)
    {
        ValidateRequest(request);

        var relevantDocuments = await RetrieveRelevantDocuments(request);
        await _responseGenerator.StreamResponseAsync(request.Message, relevantDocuments, response);
    }

    private void ValidateRequest(ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            throw new ArgumentException("Mensagem não pode estar vazia");
    }

    private string EnsureConversationId(string? conversationId)
    {
        return conversationId ?? Guid.NewGuid().ToString();
    }

    private async Task<List<DocumentChunk>> RetrieveRelevantDocuments(ChatRequest request)
    {
        if (!request.UseRAG)
        {
            _logger.LogInformation("RAG desabilitado para esta mensagem");
            return new List<DocumentChunk>();
        }

        return await _documentRetriever.RetrieveRelevantDocumentsAsync(request.Message);
    }

    private async Task<string> GenerateResponse(string userMessage, List<DocumentChunk> documents)
    {
        return await _responseGenerator.GenerateResponseAsync(userMessage, documents);
    }

    private async Task SaveConversationHistory(string conversationId, string userMessage, string botMessage)
    {
        try
        {
            await _historyService.SaveMessageAsync(conversationId, userMessage, botMessage);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao salvar histórico da conversa {ConversationId}", conversationId);
        }
    }

    private static ChatResponse CreateChatResponse(string conversationId, string botMessage,
        List<DocumentChunk> documents)
    {
        return new ChatResponse
        {
            Message = botMessage,
            ConversationId = conversationId,
            Timestamp = DateTime.UtcNow,
            SourceDocuments = documents.Select(DocumentChunkMapper.ToSourceDocument).ToList()
        };
    }
}