using ChromaDb.Responses;

namespace IA.WebApi.Services;

public interface IChromaService
{
    Task<bool> CreateCollectionAsync(string collectionName);
    Task<bool> AddDocumentsAsync(string collectionName, List<DocumentChunk> documents);
    Task<List<DocumentChunk>> SearchSimilarAsync(string collectionName, string query, int limit = 5);
    Task<bool> DeleteCollectionAsync(string collectionName);
    Task<bool> CheckCollectionExistsAsync(string collectionName);
}

// public class ChromaService : IChromaService
// {
//     private readonly HttpClient _httpClient;
//     private readonly ILogger<ChromaService> _logger;
//
//     public ChromaService(IHttpClientFactory httpClientFactory, ILogger<ChromaService> logger)
//     {
//         _httpClient = httpClientFactory.CreateClient("ChromaClient");
//         _logger = logger;
//     }
//
//     public async Task<bool> CheckCollectionExistsAsync(string collectionName)
//     {
//         try
//         {
//             // CORREÇÃO: Usar API v2
//             var response = await _httpClient.GetAsync($"/api/v2/collections/{collectionName}");
//             return response.IsSuccessStatusCode;
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "❌ Erro ao verificar se coleção existe");
//             return false;
//         }
//     }
//
//     public async Task<bool> CreateCollectionAsync(string collectionName)
//     {
//         try
//         {
//             _logger.LogInformation("🚀 Criando coleção v2: {CollectionName}", collectionName);
//
//             // Verificar se coleção já existe
//             if (await CheckCollectionExistsAsync(collectionName))
//             {
//                 _logger.LogInformation("✅ Coleção {CollectionName} já existe", collectionName);
//                 return true;
//             }
//
//             // CORREÇÃO: Usar API v2 com estrutura correta
//             var payload = new
//             {
//                 name = collectionName,
//                 metadata = new { 
//                     description = "DevOps documentation collection",
//                     created_at = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
//                 },
//                 get_or_create = true
//             };
//
//             var content = new StringContent(
//                 JsonSerializer.Serialize(payload),
//                 System.Text.Encoding.UTF8,
//                 "application/json"
//             );
//
//             // API v2: POST para criar coleção
//             var response = await _httpClient.PostAsync("/api/v2/collections", content);
//             
//             if (response.IsSuccessStatusCode)
//             {
//                 _logger.LogInformation("✅ Coleção {CollectionName} criada com API v2", collectionName);
//                 return true;
//             }
//
//             var errorContent = await response.Content.ReadAsStringAsync();
//             _logger.LogWarning("⚠️ Erro ao criar coleção v2 {CollectionName}: {StatusCode} - {Error}", 
//                 collectionName, response.StatusCode, errorContent);
//             
//             return false;
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "❌ Erro ao criar coleção {CollectionName}", collectionName);
//             return false;
//         }
//     }
//
//     public async Task<bool> AddDocumentsAsync(string collectionName, List<DocumentChunk> documents)
//     {
//         try
//         {
//             _logger.LogInformation("📝 Adicionando {Count} documentos à coleção v2 {CollectionName}", 
//                 documents.Count, collectionName);
//
//             // CORREÇÃO: Estrutura da API v2
//             var payload = new
//             {
//                 ids = documents.Select(d => d.Id).ToArray(),
//                 documents = documents.Select(d => d.Content).ToArray(),
//                 metadatas = documents.Select(d => d.Metadata).ToArray()
//             };
//
//             var content = new StringContent(
//                 JsonSerializer.Serialize(payload),
//                 System.Text.Encoding.UTF8,
//                 "application/json"
//             );
//
//             // API v2: Endpoint para adicionar
//             var response = await _httpClient.PostAsync($"/api/v2/collections/{collectionName}/add", content);
//             
//             if (response.IsSuccessStatusCode)
//             {
//                 _logger.LogInformation("✅ {Count} documentos adicionados à coleção v2 {CollectionName}", 
//                     documents.Count, collectionName);
//                 return true;
//             }
//
//             var errorContent = await response.Content.ReadAsStringAsync();
//             _logger.LogWarning("⚠️ Erro ao adicionar documentos v2: {StatusCode} - {Error}", 
//                 response.StatusCode, errorContent);
//             return false;
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "❌ Erro ao adicionar documentos à coleção {CollectionName}", collectionName);
//             return false;
//         }
//     }
//
//     public async Task<List<DocumentChunk>> SearchSimilarAsync(string collectionName, string query, int limit = 5)
//     {
//         try
//         {
//             _logger.LogInformation("🔍 Buscando documentos v2 na coleção {CollectionName} para: {Query}", 
//                 collectionName, query);
//
//             // CORREÇÃO: Estrutura da API v2
//             var payload = new
//             {
//                 query_texts = new[] { query },
//                 n_results = limit,
//                 include = new[] { "documents", "metadatas", "distances" }
//             };
//
//             var content = new StringContent(
//                 JsonSerializer.Serialize(payload),
//                 System.Text.Encoding.UTF8,
//                 "application/json"
//             );
//
//             // API v2: Endpoint para query
//             var response = await _httpClient.PostAsync($"/api/v2/collections/{collectionName}/query", content);
//             
//             if (response.IsSuccessStatusCode)
//             {
//                 var responseContent = await response.Content.ReadAsStringAsync();
//                 _logger.LogInformation("📄 Resposta ChromaDB v2: {Response}", responseContent);
//                 
//                 var result = JsonSerializer.Deserialize<ChromaQueryResponse>(responseContent);
//                 var documents = new List<DocumentChunk>();
//                 
//                 if (result?.Documents?.Any() == true && result.Documents[0]?.Any() == true)
//                 {
//                     for (int i = 0; i < result.Documents[0].Count(); i++)
//                     {
//                         documents.Add(new DocumentChunk
//                         {
//                             Id = result.Ids?[0]?[i] ?? Guid.NewGuid().ToString(),
//                             Content = result.Documents[0][i],
//                             Metadata = result.Metadatas?[0]?[i] ?? new Dictionary<string, object>(),
//                             Distance = result.Distances?[0]?[i] ?? 0.0
//                         });
//                     }
//                 }
//
//                 _logger.LogInformation("✅ Encontrados {Count} documentos similares v2", documents.Count);
//                 return documents;
//             }
//
//             var errorContent = await response.Content.ReadAsStringAsync();
//             _logger.LogWarning("⚠️ Erro na busca v2: {StatusCode} - {Error}", response.StatusCode, errorContent);
//             return new List<DocumentChunk>();
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "❌ Erro ao buscar documentos similares v2");
//             return new List<DocumentChunk>();
//         }
//     }
//
//     public async Task<bool> DeleteCollectionAsync(string collectionName)
//     {
//         try
//         {
//             // API v2: Endpoint para deletar
//             var response = await _httpClient.DeleteAsync($"/api/v2/collections/{collectionName}");
//             return response.IsSuccessStatusCode;
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "❌ Erro ao deletar coleção {CollectionName}", collectionName);
//             return false;
//         }
//     }
// }

public class DocumentChunk
{
    public string Id { get; set; } //= Guid.NewGuid().ToString();
    public string Content { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
    public double Distance { get; set; }
}

public class ChromaQueryResponse

{
    [JsonProperty("distances")] public List<List<double>>? Distances { get; set; }

    [JsonProperty("documents")] public List<List<string>> Documents { get; set; }

    [JsonProperty("embeddings")] public List<List<List<double>>> Embeddings { get; set; }

    [JsonProperty("ids")] public List<List<string>> Ids { get; set; }

    [JsonProperty("include")] public List<string> Include { get; set; }

    [JsonProperty("metadatas")] public List<List<Metadata>>? Metadatas { get; set; }

    [JsonProperty("uris")] public List<List<string>> Uris { get; set; }
}

// public class ChromaQueryResponse
// {
//     public string[]? Ids { get; set; }  // 🔥 SEM ARRAY DUPLO!
//     public string[]? Documents { get; set; }  // 🔥 SEM ARRAY DUPLO!
//     public Dictionary<string, object>[]? Metadatas { get; set; }  // 🔥 SEM ARRAY DUPLO!
//     public double[]? Distances { get; set; }  // 🔥 SEM ARRAY DUPLO!
// }

// public class ChromaQueryResponse
// {
//     public string[][]? Ids { get; set; }
//     public string[][]? Documents { get; set; }
//     public Dictionary<string, object>[][]? Metadatas { get; set; }
//     public double[][]? Distances { get; set; }
// }