using System.Text.Json;
using IA.WebApi.Controllers.Chat;

namespace IA.WebApi.Middleware;

public class OpenAICompatibilityMiddleware
{
    private readonly ILogger<OpenAICompatibilityMiddleware> _logger;
    private readonly RequestDelegate _next;

    public OpenAICompatibilityMiddleware(RequestDelegate next, ILogger<OpenAICompatibilityMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 🔄 Interceptar chamadas OpenAI
        if (context.Request.Path.StartsWithSegments("/v1/chat/completions"))
        {
            await HandleChatCompletions(context);
            return;
        }

        if (context.Request.Path.StartsWithSegments("/v1/models"))
        {
            await HandleModels(context);
            return;
        }

        await _next(context);
    }

    private async Task HandleChatCompletions(HttpContext context)
    {
        try
        {
            _logger.LogInformation("🔄 Convertendo OpenAI → DevOpsGPT");

            // Ler requisição OpenAI
            var requestBody = await ReadRequestBody(context);
            var openAIRequest = JsonSerializer.Deserialize<OpenAIRequest>(requestBody);

            // Extrair mensagem do usuário
            var userMessage = ExtractUserMessage(openAIRequest);

            _logger.LogInformation("💬 Mensagem recebida: {Message}", userMessage);

            // Converter para nosso formato
            var chatRequest = new ChatRequest
            {
                Message = userMessage,
                UseRAG = true,
                Temperature = openAIRequest?.Temperature ?? 0.7,
                Model = openAIRequest?.Model ?? "devops-gpt",
                ConversationId = "chatbot-ui-session"
            };

            // Chamar nosso serviço de chat
            var chatService = context.RequestServices.GetRequiredService<IChatService>();
            var response = await chatService.ProcessMessageAsync(chatRequest);

            // Converter resposta para formato OpenAI
            var openAIResponse = CreateOpenAIResponse(response, userMessage);

            // Retornar resposta
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(openAIResponse, JsonOptions())
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro no middleware OpenAI");
            await WriteErrorResponse(context, ex.Message);
        }
    }

    private async Task HandleModels(HttpContext context)
    {
        var response = new
        {
            @object = "list",
            data = new[]
            {
                new
                {
                    id = "devops-gpt",
                    @object = "model",
                    created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    owned_by = "devops-ai"
                }
            }
        };

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions()));
    }

    private async Task<string> ReadRequestBody(HttpContext context)
    {
        context.Request.EnableBuffering();
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        context.Request.Body.Position = 0;
        return body;
    }

    private string ExtractUserMessage(OpenAIRequest? request)
    {
        if (request?.Messages == null) return "Olá!";

        return request.Messages
            .Where(m => m.Role == "user")
            .LastOrDefault()?.Content ?? "Olá!";
    }

    private object CreateOpenAIResponse(object response, string userMessage)
    {
        // Assumindo que sua resposta tem uma propriedade Message
        var responseMessage = response.GetType().GetProperty("Message")?.GetValue(response)?.ToString() ??
                              "Erro na resposta";

        return new
        {
            id = $"devops-{Guid.NewGuid()}",
            @object = "chat.completion",
            created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            model = "devops-gpt",
            choices = new[]
            {
                new
                {
                    index = 0,
                    message = new
                    {
                        role = "assistant",
                        content = responseMessage
                    },
                    finish_reason = "stop"
                }
            },
            usage = new
            {
                prompt_tokens = userMessage.Split(' ').Length,
                completion_tokens = responseMessage.Split(' ').Length,
                total_tokens = userMessage.Split(' ').Length + responseMessage.Split(' ').Length
            }
        };
    }

    private async Task WriteErrorResponse(HttpContext context, string error)
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { error }, JsonOptions()));
    }

    private JsonSerializerOptions JsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }
}

// 📋 Modelos OpenAI
public class OpenAIRequest
{
    public string? Model { get; set; }
    public List<OpenAIMessage>? Messages { get; set; }
    public double? Temperature { get; set; }
    public bool Stream { get; set; }
}

public class OpenAIMessage
{
    public string Role { get; set; } = "";
    public string Content { get; set; } = "";
}