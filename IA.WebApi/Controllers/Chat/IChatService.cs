namespace IA.WebApi.Controllers.Chat;

public interface IChatService
{
    Task<ChatResponse> ProcessMessageAsync(ChatRequest request);
    Task StreamMessageAsync(ChatRequest request, HttpResponse response);
}