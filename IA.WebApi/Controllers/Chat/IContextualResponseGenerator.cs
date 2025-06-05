using IA.WebApi.Services;

namespace IA.WebApi.Controllers.Chat;

public interface IContextualResponseGenerator
{
    Task<string> GenerateResponseAsync(string userMessage, List<DocumentChunk> context);
    Task StreamResponseAsync(string userMessage, List<DocumentChunk> context, HttpResponse response);
}