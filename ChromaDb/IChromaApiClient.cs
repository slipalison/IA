using ChromaDb.Requests;
using ChromaDb.Responses;
using ResultPattern;

namespace ChromaDb;

public interface IChromaApiClient
{
    // Auth
    Task<Result<GetUserIdentityResponse>> GetUserIdentityAsync();

    // Health
    Task<Result<string>> HealthcheckAsync();
    Task<Result<HeartbeatResponse>> HeartbeatAsync();
    Task<Result<ChecklistResponse>> PreFlightChecksAsync();
    Task<Result<bool>> ResetAsync();
    Task<Result<string>> GetVersionAsync();

    // Tenants
    Task<Result<CreateTenantResponse>> CreateTenantAsync(CreateTenantPayload payload);
    Task<Result<GetTenantResponse>> GetTenantAsync(string tenantName);

    // Databases
    Task<Result<List<Database>>> ListDatabasesAsync(string tenant, int? limit = null, int? offset = null);
    Task<Result<CreateDatabaseResponse>> CreateDatabaseAsync(string tenant, CreateDatabasePayload payload);
    Task<Result<Database>> GetDatabaseAsync(string tenant, string database);
    Task<Result> DeleteDatabaseAsync(string tenant, string database);

    // Collections
    Task<Result<List<Collection>>> ListCollectionsAsync(string tenant, string database, int? limit = null,
        int? offset = null);

    Task<Result<Collection>> CreateCollectionAsync(string tenant, string database, CreateCollectionPayload payload);
    Task<Result<Collection>> GetCollectionAsync(string tenant, string database, string collectionId);

    Task<Result<UpdateCollectionResponse>> UpdateCollectionAsync(string tenant, string database, string collectionId,
        UpdateCollectionPayload payload);

    Task<Result> DeleteCollectionAsync(string tenant, string database, string collectionId);
    Task<Result<uint>> CountCollectionsAsync(string tenant, string database);

    Task<Result<Collection>> ForkCollectionAsync(string tenant, string database, string collectionId,
        ForkCollectionPayload payload);

    // Collection Records
    Task<Result<AddCollectionRecordsResponse>> AddRecordsAsync(string tenant, string database, string collectionId,
        AddCollectionRecordsPayload payload);

    Task<Result<uint>> CountRecordsAsync(string tenant, string database, string collectionId);

    Task<Result<DeleteCollectionRecordsResponse>> DeleteRecordsAsync(string tenant, string database,
        string collectionId, DeleteCollectionRecordsPayload payload);

    Task<Result<GetResponse>> GetRecordsAsync(string tenant, string database, string collectionId,
        GetRequestPayload payload);

    Task<Result<QueryResponse>> QueryCollectionAsync(string tenant, string database, string collectionId,
        QueryRequestPayload payload, int? limit = null, int? offset = null);

    Task<Result<UpdateCollectionRecordsResponse>> UpdateRecordsAsync(string tenant, string database,
        string collectionId, UpdateCollectionRecordsPayload payload);

    Task<Result<UpsertCollectionRecordsResponse>> UpsertRecordsAsync(string tenant, string database,
        string collectionId, UpsertCollectionRecordsPayload payload);
}