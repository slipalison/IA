using System.Text;
using System.Text.Json;
using IA.WebApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace IA.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentationController : ControllerBase
{
    private readonly IChromaService _chromaService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DocumentationController> _logger;

    public DocumentationController(
        IChromaService chromaService,
        IHttpClientFactory httpClientFactory,
        ILogger<DocumentationController> logger)
    {
        _chromaService = chromaService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpPost("initialize")]
    public async Task<IActionResult> InitializeDocumentation()
    {
        try
        {
            _logger.LogInformation("🚀 Inicializando base de conhecimento DevOps...");

            const string collectionName = "devops-docs";

            // 🔥 VERIFICAR SE JÁ TEM DOCUMENTOS NA COLEÇÃO
            if (await _chromaService.CheckCollectionExistsAsync(collectionName))
            {
                // Fazer uma busca simples para verificar se já tem conteúdo
                var existingDocs = await _chromaService.SearchSimilarAsync(collectionName, "test", 1);

                if (existingDocs.Any())
                {
                    _logger.LogInformation("✅ Coleção já inicializada com {Count} documentos", existingDocs.Count);
                    return Ok(new
                    {
                        message = "Base de conhecimento já inicializada",
                        collectionExists = true,
                        hasDocuments = true,
                        timestamp = DateTime.UtcNow
                    });
                }
            }

            // Criar coleção se não existir
            var created = await _chromaService.CreateCollectionAsync(collectionName);
            if (!created) return BadRequest("❌ Falha ao criar coleção");

            // Obter documentos base
            var documents = GetBaseDevOpsDocuments();

            // Adicionar à coleção
            var added = await _chromaService.AddDocumentsAsync(collectionName, documents);

            if (added)
            {
                _logger.LogInformation("✅ Base inicializada com {Count} documentos únicos", documents.Count);
                return Ok(new
                {
                    message = "Base de conhecimento inicializada com sucesso!",
                    documentsCount = documents.Count,
                    collectionName,
                    timestamp = DateTime.UtcNow
                });
            }

            return BadRequest("❌ Falha ao adicionar documentos");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao inicializar documentação");
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    [HttpPost("ask")]
    public async Task<IActionResult> AskDocumentation([FromBody] DocumentationRequest request)
    {
        try
        {
            _logger.LogInformation("🔍 Consultando documentação: {Question}", request.Question);

            const string collectionName = "devops-docs";

            // Buscar documentos similares
            var similarDocs = await _chromaService.SearchSimilarAsync(
                collectionName,
                request.Question,
                request.MaxResults ?? 3
            );

            if (!similarDocs.Any())
                return Ok(new
                {
                    answer =
                        "Desculpe, não encontrei informações específicas sobre sua pergunta na base de documentação. Posso ajudar com conceitos gerais de DevOps?",
                    sources = new string[0],
                    timestamp = DateTime.UtcNow
                });

            // Gerar resposta com contexto
            var contextualAnswer = await GenerateContextualAnswer(request.Question, similarDocs);

            return Ok(new
            {
                answer = contextualAnswer,
                sources = similarDocs.Select(d => new
                {
                    category = d.Metadata.GetValueOrDefault("category", "geral"),
                    title = d.Metadata.GetValueOrDefault("title", "Documento"),
                    relevance = Math.Round((1 - d.Distance ?? 0) * 100, 1)
                }),
                foundDocuments = similarDocs.Count,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao consultar documentação");
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    [HttpPost("add-document")]
    public async Task<IActionResult> AddDocument([FromBody] AddDocumentRequest request)
    {
        try
        {
            _logger.LogInformation("📝 Adicionando novo documento: {Title}", request.Title);

            const string collectionName = "devops-docs";

            var document = new DocumentChunk
            {
                Id = Guid.NewGuid().ToString(),
                Content = request.Content,
                Metadata = new Dictionary<string, object>
                {
                    { "title", request.Title },
                    { "category", request.Category ?? "custom" },
                    { "created_at", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") },
                    { "source", "user_added" }
                }
            };

            var added = await _chromaService.AddDocumentsAsync(collectionName, new List<DocumentChunk> { document });

            if (added)
                return Ok(new
                {
                    message = "✅ Documento adicionado com sucesso!",
                    documentId = document.Id,
                    title = request.Title,
                    category = request.Category
                });

            return BadRequest("Erro ao adicionar documento");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao adicionar documento");
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    [HttpPost("cleanup-duplicates")]
    public async Task<IActionResult> CleanupDuplicates()
    {
        try
        {
            _logger.LogInformation("🧹 Iniciando limpeza de documentos duplicados...");

            // Deletar coleção existente
            var deleteSuccess = await _chromaService.DeleteCollectionAsync("devops-docs");

            if (deleteSuccess)
            {
                _logger.LogInformation("✅ Coleção deletada com sucesso");

                // Aguardar um pouco
                await Task.Delay(2000);

                // Recriar com documentos únicos
                return await InitializeDocumentation();
            }

            return BadRequest("❌ Falha ao deletar coleção para limpeza");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao limpar duplicados");
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    private async Task<string> GenerateContextualAnswer(string question, List<DocumentChunk> context)
    {
        try
        {
            var contextText = string.Join("\n\n",
                context.Select(d => $"[{d.Metadata.GetValueOrDefault("category", "geral")}] {d.Content}"));

            var prompt =
                $@"Você é um especialista em DevOps que responde perguntas baseado em documentação específica. SEMPRE responda em português brasileiro.

CONTEXTO DA DOCUMENTAÇÃO:
{contextText}

PERGUNTA DO USUÁRIO: {question}

Instruções:
- Use APENAS as informações do contexto fornecido
- Responda em português brasileiro
- Seja específico e prático
- Se o contexto não contém a resposta completa, mencione isso
- Use emojis para tornar a resposta mais visual
- Cite as categorias das fontes quando relevante

RESPOSTA BASEADA NA DOCUMENTAÇÃO:";

            var client = _httpClientFactory.CreateClient("OllamaClient");
            var payload = new
            {
                model = "llama2:7b",
                prompt,
                stream = false,
                options = new
                {
                    temperature = 0.3,
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
                var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

                if (result.TryGetProperty("response", out var responseText))
                    return responseText.GetString() ?? "Erro ao processar resposta";
            }

            return "Erro ao gerar resposta contextual";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao gerar resposta contextual");
            return "Erro ao processar sua pergunta. Tente novamente.";
        }
    }

    private List<DocumentChunk> GetBaseDevOpsDocuments()
    {
        return new List<DocumentChunk>
        {
            new()
            {
                Content =
                    "Docker é uma plataforma de containerização que permite empacotar aplicações e suas dependências em containers leves e portáveis. Comandos básicos: docker build, docker run, docker ps, docker stop. Para criar um Dockerfile: FROM, COPY, RUN, EXPOSE, CMD.",
                Metadata = new Dictionary<string, object> { { "category", "containers" }, { "title", "Docker Básico" } }
            },
            new()
            {
                Content =
                    "Kubernetes é um orquestrador de containers que automatiza deploy, scaling e gerenciamento. Componentes principais: pods, services, deployments, namespaces. Comandos kubectl: kubectl get pods, kubectl apply -f, kubectl describe, kubectl logs.",
                Metadata = new Dictionary<string, object>
                    { { "category", "containers" }, { "title", "Kubernetes Essencial" } }
            },
            new()
            {
                Content =
                    "CI/CD é a prática de Continuous Integration e Continuous Deployment. Ferramentas populares: Jenkins, GitLab CI, GitHub Actions, Azure DevOps. Pipeline típico: checkout código, build, testes, deploy para staging, deploy para produção.",
                Metadata = new Dictionary<string, object> { { "category", "cicd" }, { "title", "CI/CD Pipelines" } }
            },
            new()
            {
                Content =
                    "Monitoramento DevOps envolve observabilidade de sistemas. Stack popular: Prometheus + Grafana + AlertManager. Métricas importantes: CPU, memória, disk I/O, network, latência de aplicação, error rate, throughput.",
                Metadata = new Dictionary<string, object>
                    { { "category", "monitoring" }, { "title", "Monitoramento e Observabilidade" } }
            },
            new()
            {
                Content =
                    "Infrastructure as Code (IaC) permite gerenciar infraestrutura via código. Ferramentas: Terraform, CloudFormation, Ansible, Pulumi. Benefícios: versionamento, reprodutibilidade, automação, documentação como código.",
                Metadata = new Dictionary<string, object>
                    { { "category", "iac" }, { "title", "Infrastructure as Code" } }
            },
            new()
            {
                Content =
                    "AWS oferece serviços como EC2 (compute), S3 (storage), RDS (database), VPC (network), IAM (security). Azure tem VM, Blob Storage, SQL Database, Virtual Network. GCP tem Compute Engine, Cloud Storage, Cloud SQL.",
                Metadata = new Dictionary<string, object> { { "category", "cloud" }, { "title", "Cloud Providers" } }
            }
        };
    }
}

public class DocumentationRequest
{
    public string Question { get; set; } = string.Empty;
    public int? MaxResults { get; set; } = 3;
}

public class AddDocumentRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Category { get; set; }
}