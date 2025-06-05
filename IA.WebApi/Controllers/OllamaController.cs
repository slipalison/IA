using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace IA.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OllamaController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OllamaController> _logger;

    public OllamaController(IHttpClientFactory httpClientFactory, ILogger<OllamaController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpGet("models")]
    public async Task<IActionResult> GetModels()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("OllamaClient");
            var response = await client.GetAsync("/api/tags");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return Ok(JsonSerializer.Deserialize<object>(content));
            }

            return BadRequest($"Erro ao comunicar com Ollama: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar modelos do Ollama");
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] ChatRequest2 request)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("OllamaClient");

            // 🇧🇷 PROMPT SYSTEM EM PORTUGUÊS
            var systemPrompt =
                @"Você é um assistente especializado em DevOps que SEMPRE responde em português brasileiro. 
Suas especialidades incluem:
- Automação de infraestrutura
- CI/CD pipelines
- Containerização (Docker/Kubernetes)
- Monitoramento e observabilidade
- Cloud computing (AWS, Azure, GCP)
- Análise de logs e troubleshooting
- Scripts e automação

IMPORTANTE: Sempre responda em português, mesmo que a pergunta seja em outro idioma.";

            var prompt = $"{systemPrompt}\n\nPergunta do usuário: {request.Message}\n\nResposta em português:";

            var payload = new
            {
                model = request.Model ?? "llama2:7b",
                prompt,
                stream = false,
                options = new
                {
                    temperature = 0.7,
                    top_p = 0.9,
                    num_predict = 1000
                }
            };

            var jsonContent = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/api/generate", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return Ok(JsonSerializer.Deserialize<object>(responseContent));
            }

            return BadRequest($"Erro ao gerar resposta: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar chat");
            return StatusCode(500, "Erro interno do servidor");
        }
    }
}

public class ChatRequest2
{
    public string Message { get; set; } = string.Empty;
    public string? Model { get; set; }
}