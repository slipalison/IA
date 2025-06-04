using System.ComponentModel;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ChromaDb.Responses;

// Response Models
public class GetUserIdentityResponse
{
    public string UserId { get; set; } = string.Empty;
    public string Tenant { get; set; } = string.Empty;
    public List<string> Databases { get; set; } = new();
}

public class HeartbeatResponse
{
    [JsonPropertyName("nanosecond heartbeat")]
    public long NanosecondHeartbeat { get; set; }
}

public class ChecklistResponse
{
    public int MaxBatchSize { get; set; }
}

public class CreateTenantResponse
{
}

public class GetTenantResponse
{
    public string Name { get; set; } = string.Empty;
}

public class CreateDatabaseResponse
{
}

public class DeleteDatabaseResponse
{
}

public class Database
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Tenant { get; set; } = string.Empty;
}

public class Collection
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public CollectionConfiguration ConfigurationJson { get; set; } = new();
    public string Tenant { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
    public long LogPosition { get; set; }
    public int Version { get; set; }
    public int? Dimension { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public class UpdateCollectionResponse
{
}

public class AddCollectionRecordsResponse
{
}

public class UpdateCollectionRecordsResponse
{
}

public class UpsertCollectionRecordsResponse
{
}

public class DeleteCollectionRecordsResponse
{
}

public class GetResponse
{
    public List<string> Ids { get; set; } = new();
    public List<Include> Include { get; set; } = new();
    public List<string?>? Documents { get; set; }
    public List<List<double>>? Embeddings { get; set; }
    public List<Dictionary<string, object>?>? Metadatas { get; set; }
    public List<string?>? Uris { get; set; }
}

public class QueryResponse
{
    [JsonProperty("ids")]
    [JsonPropertyName("ids")]
    public List<List<string>>? Ids { get; set; }

    [JsonProperty("embeddings")]
    [JsonPropertyName("embeddings")]
    //public object Embeddings { get; set; }
    public List<List<List<double>?>>? Embeddings { get; set; }

    [JsonProperty("documents")]
    [JsonPropertyName("documents")]
    public List<List<string>>? Documents { get; set; }

    [JsonProperty("uris")]
    [JsonPropertyName("uris")]
    public List<List<string?>>? Uris { get; set; }

    [JsonProperty("metadatas")]
    [JsonPropertyName("metadatas")]
    public List<List<Metadata>>? Metadatas { get; set; }

    [JsonProperty("distances")]
    [JsonPropertyName("distances")]
    public List<List<double>>? Distances { get; set; }

    [JsonProperty("include")]
    [JsonPropertyName("include")]
    public List<string>? Include { get; set; }
}

// public class QueryResponse
// {
//     public List<List<string>> Ids { get; set; } = new();
//     public List<Include> Include { get; set; } = new();
//     public List<List<double?>>? Distances { get; set; }
//     public List<List<string?>>? Documents { get; set; }
//     public List<List<List<double>?>>? Embeddings { get; set; }
//     public List<List<Metadata>>? Metadatas { get; set; }
//     public List<List<string?>>? Uris { get; set; }
// }

public class Metadata
{
    [JsonProperty("embedding_dimension")] public int EmbeddingDimension { get; set; }

    [JsonProperty("added_at")] public string AddedAt { get; set; }

    [JsonProperty("added_by")] public string AddedBy { get; set; }

    [JsonProperty("embedding_model")] public string EmbeddingModel { get; set; }

    [JsonProperty("batch_id")] public string BatchId { get; set; }

    [JsonProperty("title")] public string Title { get; set; }

    [JsonProperty("category")] public string Category { get; set; }
}

public class ErrorResponse
{
    public string Error { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public enum Include
{
    [Description("uris")] Uris,
    [Description("documents")] Documents,
    [Description("metadatas")] Metadatas,
    [Description("distances")] Distances,
    [Description("embeddings")] Embeddings
}

public class CollectionConfiguration
{
    public EmbeddingFunctionConfiguration? EmbeddingFunction { get; set; }
    public HnswConfiguration? Hnsw { get; set; }
    public SpannConfiguration? Spann { get; set; }
}

public class EmbeddingFunctionConfiguration
{
    public string Type { get; set; } = string.Empty;
    public string? Name { get; set; }
    public object? Config { get; set; }
}

public class HnswConfiguration
{
    public int? EfConstruction { get; set; }
    public int? EfSearch { get; set; }
    public int? MaxNeighbors { get; set; }
    public double? ResizeFactor { get; set; }
    public HnswSpace? Space { get; set; }
    public int? SyncThreshold { get; set; }
}

public class UpdateHnswConfiguration
{
    public int? BatchSize { get; set; }
    public int? EfSearch { get; set; }
    public int? MaxNeighbors { get; set; }
    public int? NumThreads { get; set; }
    public double? ResizeFactor { get; set; }
    public int? SyncThreshold { get; set; }
}

public class SpannConfiguration
{
    public int? EfConstruction { get; set; }
    public int? EfSearch { get; set; }
    public int? MaxNeighbors { get; set; }
    public int? MergeThreshold { get; set; }
    public int? ReassignNeighborCount { get; set; }
    public int? SearchNprobe { get; set; }
    public HnswSpace? Space { get; set; }
    public int? SplitThreshold { get; set; }
    public int? WriteNprobe { get; set; }
}

public enum HnswSpace
{
    L2,
    Cosine,
    Ip
}