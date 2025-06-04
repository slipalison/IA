using ChromaDb.Responses;
using Newtonsoft.Json;

namespace IA.WebApi.Services;

public interface IChromaService
{
    Task<bool> CreateCollectionAsync(string collectionName);
    Task<bool> AddDocumentsAsync(string collectionName, List<DocumentChunk> documents);
    Task<List<DocumentChunk>> SearchSimilarAsync(string collectionName, string query, int limit = 5);
    Task<bool> DeleteCollectionAsync(string collectionName);
    Task<bool> CheckCollectionExistsAsync(string collectionName);
}

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