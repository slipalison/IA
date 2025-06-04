using System.Text.Json.Serialization;
using ChromaDb.Responses;

namespace ChromaDb.Requests;

// Request Models
public class CreateTenantPayload
{
    public string Name { get; set; } = string.Empty;
}

public class CreateDatabasePayload
{
    public string Name { get; set; } = string.Empty;
}

public class CreateCollectionPayload
{
    public string Name { get; set; } = string.Empty;
    public bool GetOrCreate { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public CollectionConfiguration? Configuration { get; set; }
}

public class UpdateCollectionPayload
{
    public string? NewName { get; set; }
    public Dictionary<string, object>? NewMetadata { get; set; }
    public UpdateCollectionConfiguration? NewConfiguration { get; set; }
}

public class ForkCollectionPayload
{
    public string NewName { get; set; } = string.Empty;
}

public class AddCollectionRecordsPayload
{
    public List<string> Ids { get; set; } = new();
    public List<string?>? Documents { get; set; }
    public List<List<float>>? Embeddings { get; set; }
    public List<Dictionary<string, object>?>? Metadatas { get; set; }
    public List<string?>? Uris { get; set; }
}

public class UpdateCollectionRecordsPayload
{
    public List<string> Ids { get; set; } = new();
    public List<string?>? Documents { get; set; }
    public List<List<float>?>? Embeddings { get; set; }
    public List<Dictionary<string, object>?>? Metadatas { get; set; }
    public List<string?>? Uris { get; set; }
}

public class UpsertCollectionRecordsPayload
{
    public List<string> Ids { get; set; } = new();
    public List<string?>? Documents { get; set; }
    public List<List<float>>? Embeddings { get; set; }
    public List<Dictionary<string, object>?>? Metadatas { get; set; }
    public List<string?>? Uris { get; set; }
}

public class DeleteCollectionRecordsPayload
{
    public List<string>? Ids { get; set; }
    public object? Where { get; set; }
    public object? WhereDocument { get; set; }
}

public class GetRequestPayload
{
    public List<string>? Ids { get; set; }
    public object? Where { get; set; }
    public object? WhereDocument { get; set; }
    public List<Include> Include { get; set; } = new();
    public int? Limit { get; set; }
    public int? Offset { get; set; }
}

public class QueryRequestPayload
{
    [JsonPropertyName("query_embeddings")] public List<List<float>> QueryEmbeddings { get; set; } = new();
    [JsonPropertyName("ids")] public List<string>? Ids { get; set; }
    [JsonPropertyName("where")] public object? Where { get; set; }
    [JsonPropertyName("where_document")] public object? WhereDocument { get; set; }

    [JsonPropertyName("n_results")] public int? NResults { get; set; }

    [JsonIgnore] public List<Include>? Includes { get; set; }

    //[JsonPropertyName("include")] public List<Include> Include { get; set; } = new();

    [JsonPropertyName("include")]
    public List<string>? Include
    {
        get => Includes?.Select(x => x.ToString().ToLower()).ToList();
        set
        {
            Includes ??= new List<Include>();
            Includes = value.Select(x => Enum.Parse<Include>(x)).ToList();
        }
    }
}

public class UpdateCollectionConfiguration
{
    public EmbeddingFunctionConfiguration? EmbeddingFunction { get; set; }
    public UpdateHnswConfiguration? Hnsw { get; set; }
    public SpannConfiguration? Spann { get; set; }
}