using ChromaDb.Requests;
using ChromaDb.Responses;
using Flurl.Http;
using ResultPattern;

namespace ChromaDb;

public class ChromaApiClient : IChromaApiClient
{
    private readonly string? _apiKey;
    private readonly string _baseUrl;
    private readonly IFlurlClient _flulrClient;
    private readonly HttpClient _httpClient;

    public ChromaApiClient(HttpClient httpClient, string? apiKey = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _apiKey = apiKey;

        _flulrClient = new FlurlClient(httpClient);
    }


    // Auth
    public async Task<Result<GetUserIdentityResponse>> GetUserIdentityAsync()
    {
        return await CreateRequest("api/v2/auth/identity").GetJsonResultAsync<GetUserIdentityResponse>();
    }

    // Health
    public async Task<Result<string>> HealthcheckAsync()
    {
        return await CreateRequest("api/v2/healthcheck").GetJsonResultAsync<string>();
    }

    public async Task<Result<HeartbeatResponse>> HeartbeatAsync()
    {
        return await CreateRequest("api/v2/heartbeat").GetJsonResultAsync<HeartbeatResponse>();
    }

    public async Task<Result<ChecklistResponse>> PreFlightChecksAsync()
    {
        return await CreateRequest("api/v2/pre-flight-checks").GetJsonResultAsync<ChecklistResponse>();
    }

    public async Task<Result<bool>> ResetAsync()
    {
        return await CreateRequest("api/v2/reset").PostJsonResultAsync<bool>();
    }

    public async Task<Result<string>> GetVersionAsync()
    {
        return await CreateRequest("api/v2/version").GetJsonResultAsync<string>();
    }

    // Tenants
    public async Task<Result<CreateTenantResponse>> CreateTenantAsync(CreateTenantPayload payload)
    {
        return await CreateRequest("api/v2/tenants").PostJsonResultAsync<CreateTenantResponse>(payload);
    }

    public async Task<Result<GetTenantResponse>> GetTenantAsync(string tenantName)
    {
        return await CreateRequest($"api/v2/tenants/{tenantName}").GetJsonResultAsync<GetTenantResponse>();
    }

    // Databases
    public async Task<Result<List<Database>>> ListDatabasesAsync(string tenant, int? limit = null, int? offset = null)
    {
        var request = CreateRequest($"api/v2/tenants/{tenant}/databases");

        if (limit.HasValue)
            request = request.SetQueryParam("limit", limit.Value);
        if (offset.HasValue)
            request = request.SetQueryParam("offset", offset.Value);

        return await request.GetJsonResultAsync<List<Database>>();
    }

    public async Task<Result<CreateDatabaseResponse>> CreateDatabaseAsync(string tenant, CreateDatabasePayload payload)
    {
        return await CreateRequest($"api/v2/tenants/{tenant}/databases")
            .PostJsonResultAsync<CreateDatabaseResponse>(payload);
    }

    public async Task<Result<Database>> GetDatabaseAsync(string tenant, string database)
    {
        return await CreateRequest($"api/v2/tenants/{tenant}/databases/{database}")
            .GetJsonResultAsync<Database>();
    }

    public async Task<Result> DeleteDatabaseAsync(string tenant, string database)
    {
        return await CreateRequest($"api/v2/tenants/{tenant}/databases/{database}")
            .DeleteResultAsync();
    }

    // Collections
    public async Task<Result<List<Collection>>> ListCollectionsAsync(string tenant, string database, int? limit = null,
        int? offset = null)
    {
        var request = CreateRequest($"api/v2/tenants/{tenant}/databases/{database}/collections");

        if (limit.HasValue)
            request = request.SetQueryParam("limit", limit.Value);
        if (offset.HasValue)
            request = request.SetQueryParam("offset", offset.Value);

        return await request.GetJsonResultAsync<List<Collection>>();
    }

    public async Task<Result<Collection>> CreateCollectionAsync(string tenant, string database,
        CreateCollectionPayload payload)
    {
        return await CreateRequest($"api/v2/tenants/{tenant}/databases/{database}/collections")
            .PostJsonResultAsync<Collection>(payload);
    }

    public async Task<Result<Collection>> GetCollectionAsync(string tenant, string database, string collectionId)
    {
        return await CreateRequest($"api/v2/tenants/{tenant}/databases/{database}/collections/{collectionId}")
            .GetJsonResultAsync<Collection>();
    }

    public async Task<Result<UpdateCollectionResponse>> UpdateCollectionAsync(string tenant, string database,
        string collectionId, UpdateCollectionPayload payload)
    {
        return await CreateRequest($"api/v2/tenants/{tenant}/databases/{database}/collections/{collectionId}")
            .PutJsonResultAsync<UpdateCollectionResponse>(payload);
    }

    public async Task<Result> DeleteCollectionAsync(string tenant, string database,
        string collectionId)
    {
        return await CreateRequest($"api/v2/tenants/{tenant}/databases/{database}/collections/{collectionId}")
            .DeleteResultAsync();
    }

    public async Task<Result<uint>> CountCollectionsAsync(string tenant, string database)
    {
        return await CreateRequest($"api/v2/tenants/{tenant}/databases/{database}/collections_count")
            .GetJsonResultAsync<uint>();
    }

    public async Task<Result<Collection>> ForkCollectionAsync(string tenant, string database, string collectionId,
        ForkCollectionPayload payload)
    {
        return await CreateRequest($"api/v2/tenants/{tenant}/databases/{database}/collections/{collectionId}/fork")
            .PostJsonResultAsync<Collection>(payload);
    }

    // Collection Records
    public async Task<Result<AddCollectionRecordsResponse>> AddRecordsAsync(string tenant, string database,
        string collectionId,
        AddCollectionRecordsPayload payload)
    {
        return await CreateRequest($"api/v2/tenants/{tenant}/databases/{database}/collections/{collectionId}/add")
            .PostJsonResultAsync<AddCollectionRecordsResponse>(payload);
    }

    public async Task<Result<uint>> CountRecordsAsync(string tenant, string database, string collectionId)
    {
        return await CreateRequest($"api/v2/tenants/{tenant}/databases/{database}/collections/{collectionId}/count")
            .GetJsonResultAsync<uint>();
    }

    public async Task<Result<DeleteCollectionRecordsResponse>> DeleteRecordsAsync(string tenant, string database,
        string collectionId, DeleteCollectionRecordsPayload payload)
    {
        return await CreateRequest($"api/v2/tenants/{tenant}/databases/{database}/collections/{collectionId}/delete")
            .PostJsonResultAsync<DeleteCollectionRecordsResponse>(payload);
    }

    public async Task<Result<GetResponse>> GetRecordsAsync(string tenant, string database, string collectionId,
        GetRequestPayload payload)
    {
        return await CreateRequest($"api/v2/tenants/{tenant}/databases/{database}/collections/{collectionId}/get")
            .PostJsonResultAsync<GetResponse>(payload);
    }

    public async Task<Result<QueryResponse>> QueryCollectionAsync(string tenant, string database, string collectionId,
        QueryRequestPayload payload, int? limit = null, int? offset = null)
    {
        var request = CreateRequest($"api/v2/tenants/{tenant}/databases/{database}/collections/{collectionId}/query");

        if (limit.HasValue)
            request = request.SetQueryParam("limit", limit.Value);
        if (offset.HasValue)
            request = request.SetQueryParam("offset", offset.Value);

        return await request.PostJsonResultAsync<QueryResponse>(payload);
    }

    public async Task<Result<UpdateCollectionRecordsResponse>> UpdateRecordsAsync(string tenant, string database,
        string collectionId, UpdateCollectionRecordsPayload payload)
    {
        return await CreateRequest($"api/v2/tenants/{tenant}/databases/{database}/collections/{collectionId}/update")
            .PostJsonResultAsync<UpdateCollectionRecordsResponse>(payload);
    }

    public async Task<Result<UpsertCollectionRecordsResponse>> UpsertRecordsAsync(string tenant, string database,
        string collectionId, UpsertCollectionRecordsPayload payload)
    {
        return await CreateRequest($"api/v2/tenants/{tenant}/databases/{database}/collections/{collectionId}/upsert")
            .PostJsonResultAsync<UpsertCollectionRecordsResponse>(payload);
    }

    private IFlurlRequest CreateRequest(string endpoint)
    {
        var request = _flulrClient.Request(endpoint).AllowAnyHttpStatus();

        if (!string.IsNullOrEmpty(_apiKey)) request = request.WithHeader("x-chroma-token", _apiKey);

        return request;
    }
}