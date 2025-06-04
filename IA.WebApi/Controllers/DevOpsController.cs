using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace IA.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DevOpsController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DevOpsController> _logger;

    public DevOpsController(ILogger<DevOpsController> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    [HttpPost("analyze-logs")]
    public async Task<IActionResult> AnalyzeLogs([FromBody] LogAnalysisRequest request)
    {
        try
        {
            _logger.LogInformation("🔍 Analisando logs...");

            var prompt =
                $@"Você é um especialista em DevOps e deve analisar logs de sistema. SEMPRE responda em português brasileiro.

Analise os seguintes logs e forneça:
1. 🔍 PROBLEMAS IDENTIFICADOS: Liste todos os erros e problemas encontrados
2. 📊 PADRÕES ANÔMALOS: Identifique comportamentos suspeitos ou fora do normal  
3. ✅ RECOMENDAÇÕES: Ações específicas para resolver os problemas
4. 🚨 SEVERIDADE: Classifique como CRÍTICO, ALTO, MÉDIO ou BAIXO

LOGS PARA ANÁLISE:
{request.LogContent}

Responda em formato JSON estruturado EM PORTUGUÊS:";

            var client = _httpClientFactory.CreateClient("OllamaClient");
            var payload = new
            {
                model = "llama2:7b",
                prompt,
                stream = false,
                options = new
                {
                    temperature = 0.3, // Mais determinístico para análise técnica
                    top_p = 0.9,
                    num_predict = 1500
                }
            };

            var jsonContent = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/api/generate", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return Ok(new
                {
                    analysis = JsonSerializer.Deserialize<object>(responseContent),
                    timestamp = DateTime.UtcNow,
                    logSize = request.LogContent.Length,
                    language = "pt-BR"
                });
            }

            return BadRequest("Erro ao analisar logs");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao analisar logs");
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    [HttpPost("generate-script")]
    public async Task<IActionResult> GenerateScript([FromBody] ScriptGenerationRequest request)
    {
        try
        {
            _logger.LogInformation("🔧 Gerando script DevOps...");

            var prompt =
                $@"Você é um especialista em DevOps que cria scripts de automação. SEMPRE responda em português brasileiro.

Gere um script {request.ScriptType} para: {request.Description}

REQUISITOS OBRIGATÓRIOS:
✅ Comentários explicativos EM PORTUGUÊS
✅ Tratamento de erros robusto
✅ Validações de entrada
✅ Logs informativos
✅ Boas práticas de segurança
✅ Código limpo e legível

ESTRUTURA DO SCRIPT:
1. Cabeçalho com descrição
2. Validações iniciais
3. Funções principais
4. Tratamento de erros
5. Logs e outputs

TIPO: {request.ScriptType}
DESCRIÇÃO: {request.Description}

Gere o script completo com comentários EM PORTUGUÊS:";

            var client = _httpClientFactory.CreateClient("OllamaClient");
            var payload = new
            {
                model = "codellama:7b",
                prompt,
                stream = false,
                options = new
                {
                    temperature = 0.2, // Baixa para código mais consistente
                    top_p = 0.8,
                    num_predict = 2000
                }
            };

            var jsonContent = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/api/generate", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return Ok(new
                {
                    script = JsonSerializer.Deserialize<object>(responseContent),
                    scriptType = request.ScriptType,
                    timestamp = DateTime.UtcNow,
                    language = "pt-BR"
                });
            }

            return BadRequest("Erro ao gerar script");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar script");
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    [HttpPost("troubleshoot")]
    public async Task<IActionResult> Troubleshoot([FromBody] TroubleshootRequest request)
    {
        try
        {
            _logger.LogInformation("🔧 Iniciando troubleshooting...");

            var prompt = $@"Você é um especialista em troubleshooting DevOps. SEMPRE responda em português brasileiro.

PROBLEMA RELATADO: {request.Problem}
CONTEXTO: {request.Context ?? "Não informado"}

Forneça um guia completo de troubleshooting:

🔍 DIAGNÓSTICO INICIAL:
- Possíveis causas do problema
- Informações adicionais necessárias

🔧 PASSOS DE RESOLUÇÃO:
1. Verificações básicas
2. Comandos de diagnóstico
3. Soluções passo-a-passo

📊 PREVENÇÃO:
- Como evitar o problema no futuro
- Monitoramento recomendado

Responda EM PORTUGUÊS com instruções claras e práticas e completas:";

            var client = _httpClientFactory.CreateClient("OllamaClient");
            var payload = new
            {
                model = "llama2:7b",
                prompt,
                stream = false,
                options = new
                {
                    temperature = 0.4,
                    top_p = 0.9,
                    num_predict = 1800
                }
            };

            var jsonContent = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/api/generate", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return Ok(new
                {
                    troubleshooting = JsonSerializer.Deserialize<object>(responseContent),
                    problem = request.Problem,
                    timestamp = DateTime.UtcNow,
                    language = "pt-BR"
                });
            }

            return BadRequest("Erro ao processar troubleshooting");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar troubleshooting");
            return StatusCode(500, "Erro interno do servidor");
        }
    }
}

public class LogAnalysisRequest
{
    public string LogContent { get; set; } = string.Empty;
}

public class ScriptGenerationRequest
{
    public string Description { get; set; } = string.Empty;
    public string ScriptType { get; set; } = "bash";
}

public class TroubleshootRequest
{
    public string Problem { get; set; } = string.Empty;
    public string? Context { get; set; }
}