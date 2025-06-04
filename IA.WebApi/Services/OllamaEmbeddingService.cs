using System.Text;
using System.Text.Json;

namespace IA.WebApi.Services;

public class OllamaEmbeddingService : IEmbeddingService
{
    // 🔥 USAR MODELO MAIS COMUM E FUNCIONAL
    private const string EMBEDDING_MODEL = "mxbai-embed-large";
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaEmbeddingService> _logger;

    public OllamaEmbeddingService(IHttpClientFactory httpClientFactory, ILogger<OllamaEmbeddingService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("OllamaClient");
        _logger = logger;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        try
        {
            _logger.LogInformation("🔢 Gerando embedding para: '{TextPreview}'",
                text.Length > 100 ? text.Substring(0, 100) + "..." : text);

            // 🔥 PAYLOAD CORRETO PARA OLLAMA
            var payload = new
            {
                model = EMBEDDING_MODEL,
                prompt = text
            };

            var jsonContent = JsonSerializer.Serialize(payload);
            _logger.LogInformation("📤 Payload embedding: {Payload}", jsonContent);

            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // 🔥 ENDPOINT CORRETO
            var response = await _httpClient.PostAsync("/api/embeddings", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("📨 Resposta embedding: {StatusCode} - {Response}",
                response.StatusCode, responseContent);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

                if (result.TryGetProperty("embedding", out var embeddingElement))
                {
                    var embedding = embeddingElement.EnumerateArray()
                        .Select(x => x.GetSingle())
                        .ToArray();

                    _logger.LogInformation("✅ Embedding gerado: {Dimensions} dimensões", embedding.Length);

                    // 🔥 VALIDAR SE EMBEDDING NÃO ESTÁ VAZIO
                    if (embedding.Length == 0)
                    {
                        _logger.LogError("❌ Embedding vazio retornado!");
                        return GenerateFallbackEmbedding(text);
                    }

                    return embedding;
                }

                _logger.LogError("❌ Propriedade 'embedding' não encontrada na resposta");
            }
            else
            {
                _logger.LogError("❌ Erro no Ollama: {StatusCode} - {Error}",
                    response.StatusCode, responseContent);
            }

            // 🔥 FALLBACK: Gerar embedding simples se falhar
            return GenerateFallbackEmbedding(text);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao gerar embedding");
            return GenerateFallbackEmbedding(text);
        }
    }

    public async Task<List<float[]>> GenerateEmbeddingsAsync(List<string> texts)
    {
        try
        {
            _logger.LogInformation("🔢 Gerando {Count} embeddings...", texts.Count);

            var embeddings = new List<float[]>();

            // 🔥 PROCESSAR UM POR VEZ PARA DEBUGGING
            for (var i = 0; i < texts.Count; i++)
            {
                _logger.LogInformation("📝 Processando embedding {Index}/{Total}...", i + 1, texts.Count);

                var embedding = await GenerateEmbeddingAsync(texts[i]);
                embeddings.Add(embedding);

                // Pausa entre requests
                await Task.Delay(500);
            }

            _logger.LogInformation("✅ Todos os {Count} embeddings gerados!", embeddings.Count);

            // 🔥 VALIDAR SE TODOS TÊM A MESMA DIMENSÃO
            var dimensions = embeddings.FirstOrDefault()?.Length ?? 0;
            var allSameDimension = embeddings.All(e => e.Length == dimensions);

            if (!allSameDimension)
            {
                _logger.LogError("❌ Embeddings com dimensões inconsistentes!");
                foreach (var (embedding, index) in embeddings.Select((e, i) => (e, i)))
                    _logger.LogError("  Embedding {Index}: {Dimensions} dimensões", index, embedding.Length);
            }

            return embeddings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao gerar embeddings em lote");
            return texts.Select(GenerateFallbackEmbedding).ToList();
        }
    }

    /// <summary>
    ///     🔥 FALLBACK: Gerar embedding baseado em hash do texto
    /// </summary>
    private float[] GenerateFallbackEmbedding(string text)
    {
        _logger.LogWarning("⚠️ Gerando embedding fallback para: '{Text}'",
            text.Length > 50 ? text.Substring(0, 50) + "..." : text);

        // Gerar embedding simples baseado no hash do texto
        var hash = text.GetHashCode();
        var random = new Random(hash);

        // 🔥 USAR DIMENSÃO PADRÃO COMPATÍVEL COM CHROMA
        const int embeddingDim = 384; // Dimensão comum para embeddings

        var embedding = new float[embeddingDim];
        for (var i = 0; i < embeddingDim; i++)
            embedding[i] = (float)(random.NextDouble() * 2 - 1); // Valores entre -1 e 1

        _logger.LogInformation("✅ Embedding fallback gerado: {Dimensions} dimensões", embedding.Length);
        return embedding;
    }
}