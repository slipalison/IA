using System.Text;
using System.Text.Json;
using IA.WebApi.Services;

namespace IA.WebApi.Controllers.Chat;

public class ContextualResponseGenerator : IContextualResponseGenerator
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ContextualResponseGenerator> _logger;
    private readonly IPromptBuilder _promptBuilder;
    private readonly IResponseCleaner _responseCleaner;

    public ContextualResponseGenerator(
        IHttpClientFactory httpClientFactory,
        IPromptBuilder promptBuilder,
        IResponseCleaner responseCleaner,
        ILogger<ContextualResponseGenerator> logger)
    {
        _httpClientFactory = httpClientFactory;
        _promptBuilder = promptBuilder;
        _responseCleaner = responseCleaner;
        _logger = logger;
    }

    public async Task<string> GenerateResponseAsync(string userMessage, List<DocumentChunk> context)
    {
        try
        {
            var prompt = _promptBuilder.BuildPrompt(userMessage, context);
            var ollamaResponse = await CallOllamaAPI(prompt, false);

            return _responseCleaner.CleanResponse(ollamaResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar resposta contextual");
            return "Desculpe, ocorreu um erro ao processar sua mensagem. Pode tentar novamente?";
        }
    }

    public async Task StreamResponseAsync(string userMessage, List<DocumentChunk> context, HttpResponse response)
    {
        var prompt = _promptBuilder.BuildPrompt(userMessage, context);
        await StreamOllamaResponse(prompt, response);
    }

    private async Task<string> CallOllamaAPI(string prompt, bool stream)
    {
        var client = _httpClientFactory.CreateClient("OllamaClient");
        var payload = CreateOllamaPayload(prompt, stream);

        var jsonContent = JsonSerializer.Serialize(payload);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        _logger.LogInformation("Enviando requisição para Ollama");
        var response = await client.PostAsync("/api/generate", content);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Ollama retornou erro: {StatusCode}", response.StatusCode);
            throw new InvalidOperationException($"Erro na API Ollama: {response.StatusCode}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var ollamaResponse = JsonSerializer.Deserialize<OllamaResponse>(responseContent);

        return ollamaResponse?.Response ?? string.Empty;
    }

    private async Task StreamOllamaResponse(string prompt, HttpResponse response)
    {
        var client = _httpClientFactory.CreateClient("OllamaClient");
        var payload = CreateOllamaPayload(prompt, true);

        var jsonContent = JsonSerializer.Serialize(payload);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        using var ollamaResponse = await client.PostAsync("/api/generate", content);
        using var stream = await ollamaResponse.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (!string.IsNullOrWhiteSpace(line))
            {
                await response.WriteAsync($"data: {line}\n\n");
                await response.Body.FlushAsync();
            }
        }
    }

    private static object CreateOllamaPayload(string prompt, bool stream)
    {
        return new
        {
            model = "llama2:7b",
            prompt,
            stream,
            options = new
            {
                temperature = 0.7,
                top_p = 0.9,
                num_predict = 1500,
                stop = new[] { "\n\nUsuário:", "\n\nUser:" }
            }
        };
    }
}