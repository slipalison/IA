using IA.WebApi.Services;

namespace IA.WebApi.Controllers.Chat;

public interface IPromptBuilder
{
    string BuildPrompt(string userMessage, List<DocumentChunk> context);
}