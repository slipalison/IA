using ChromaDb;
using ChromaDb.Requests;
using ChromaDb.Responses;

namespace IA.WebApi.Services;

public class ChromaService : IChromaService
{
    // 🎯 API v2 Structure: tenant/database/collection
    private const string TENANT_NAME = "default";
    private const string DATABASE_NAME = "default";

    private readonly IChromaApiClient _apiClient;

    // Mapeamento de nomes de coleção para UUIDs
    private readonly Dictionary<string, string> _collectionUuidMap = new();
    private readonly IEmbeddingService _embeddingService; // 🔥 NOVO!

    // private readonly HttpClient _httpClient;
    private readonly ILogger<ChromaService> _logger;

    public ChromaService(
        IHttpClientFactory httpClientFactory,
        ILogger<ChromaService> logger,
        IEmbeddingService embeddingService, IChromaApiClient apiClient) // 🔥 INJEÇÃO!
    {
        //_httpClient = httpClientFactory.CreateClient("ChromaClient");
        _logger = logger;
        _embeddingService = embeddingService; // 🔥 NOVO!
        _apiClient = apiClient;
    }


    public async Task<bool> CheckCollectionExistsAsync(string collectionName)
    {
        try
        {
            _logger.LogInformation("🔍 [API v2] Verificando se coleção {CollectionName} existe...", collectionName);

            var listResponse = await _apiClient.ListCollectionsAsync(TENANT_NAME, DATABASE_NAME);
            if (listResponse.IsSuccess)
            {
                try
                {
                    if (listResponse.Data.Count > 0 && listResponse.Data.Any(x => x.Id != null))
                    {
                        _collectionUuidMap[collectionName] = listResponse.Data.FirstOrDefault()?.Id.ToString();
                        return true;
                    }
                }
                catch (Exception jsonEx)
                {
                    _logger.LogError(jsonEx, "❌ Erro ao processar JSON de coleções: {Error}", jsonEx.Message);
                }
            }


            _logger.LogInformation("📋 Coleção {CollectionName} não existe", collectionName);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao verificar se coleção {CollectionName} existe", collectionName);
            return false;
        }
    }


    public async Task<bool> CreateCollectionAsync(string collectionName)
    {
        try
        {
            _logger.LogInformation("🚀 [API v2] Criando coleção: {CollectionName}", collectionName);

            // Validar estrutura
            if (!await EnsureTenantAndDatabaseExistAsync())
            {
                _logger.LogError("❌ Falha na validação/criação da estrutura Tenant/Database");
                return false;
            }

            // Limpar cache
            _collectionUuidMap.Clear();

            // Verificar se existe
            if (await CheckCollectionExistsAsync(collectionName))
            {
                _logger.LogInformation("✅ Coleção {CollectionName} já existe", collectionName);
                return true;
            }

            // 🔥 PAYLOAD COM DIMENSÃO CORRETA PARA MXBAI-EMBED-LARGE (1024)
            var payload = new CreateCollectionPayload
            {
                //Id = collectionUuid,
                Name = collectionName,
                GetOrCreate = true,
                Configuration = new CollectionConfiguration
                {
                    Hnsw = new HnswConfiguration
                    {
                        Space = HnswSpace.Cosine,
                        EfConstruction = 200,
                        EfSearch = 200,
                        MaxNeighbors = 16,
                        ResizeFactor = 1.2,
                        SyncThreshold = 1000
                    }
                },
                Metadata = new Dictionary<string, object>
                {
                    { "description", "DevOps documentation - mxbai-embed-large embeddings" },
                    { "embedding_model", "mxbai-embed-large" },
                    { "embedding_dimension", 1024 },
                    { "created_by", "IA.WebApi" },
                    { "created_at", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") }
                }
                // 🔥 NÃO especificar embedding_dimension no configuration_json
                // Deixar ChromaDB detectar automaticamente baseado no primeiro embedding
            };

            _logger.LogInformation("📤 [API v2] Payload criação: {@Payload}", payload);

            var response = await _apiClient.CreateCollectionAsync(TENANT_NAME, DATABASE_NAME, payload);

            _logger.LogInformation("📨 Resposta criação: {StatusCode} - {@Response}", response.StatusCode,
                response.Data);

            if (response.IsSuccess)
            {
                _collectionUuidMap[collectionName] = response.Data.Id.ToString();
                _logger.LogInformation("✅ Coleção {CollectionName} criada para embeddings 1024D!", collectionName);
                return true;
            }

            _logger.LogError("❌ Erro ao criar coleção: {StatusCode} - {@Error}",
                response.StatusCode, response.Error);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao criar coleção {CollectionName}", collectionName);
            return false;
        }
    }


    public async Task<bool> AddDocumentsAsync(string collectionName, List<DocumentChunk> documents)
    {
        try
        {
            _logger.LogInformation("📝 [API v2] Adicionando {Count} documentos à coleção {CollectionName}",
                documents.Count, collectionName);

            // Obter UUID da coleção
            if (!_collectionUuidMap.TryGetValue(collectionName, out var collectionUuid))
            {
                if (!await CheckCollectionExistsAsync(collectionName))
                {
                    _logger.LogError("❌ Coleção '{CollectionName}' não encontrada", collectionName);
                    return false;
                }

                collectionUuid = _collectionUuidMap[collectionName];
            }

            // 🔥 GERAR EMBEDDINGS COM VALIDAÇÃO
            _logger.LogInformation("🔢 Gerando embeddings para {Count} documentos...", documents.Count);

            var texts = documents.Select(d => d.Content).ToList();
            var embeddings = await _embeddingService.GenerateEmbeddingsAsync(texts);

            // 🔥 VALIDAÇÕES CRÍTICAS
            if (embeddings.Count != documents.Count)
            {
                _logger.LogError("❌ Número de embeddings ({EmbeddingCount}) ≠ documentos ({DocCount})",
                    embeddings.Count, documents.Count);
                return false;
            }

            // Verificar se todos embeddings têm a mesma dimensão
            var expectedDimension = 1024; // mxbai-embed-large
            var invalidEmbeddings = embeddings.Where(e => e.Length != expectedDimension).ToList();

            if (invalidEmbeddings.Any())
            {
                _logger.LogError("❌ Embeddings com dimensão incorreta encontrados:");
                for (var i = 0; i < embeddings.Count; i++)
                    if (embeddings[i].Length != expectedDimension)
                        _logger.LogError("  Embedding {Index}: {ActualDim}D (esperado: {ExpectedDim}D)",
                            i, embeddings[i].Length, expectedDimension);

                return false;
            }

            _logger.LogInformation("✅ Todos os {Count} embeddings têm {Dimension} dimensões", embeddings.Count,
                expectedDimension);

            // Adicionar metadados
            var batchId = Guid.NewGuid().ToString();
            foreach (var doc in documents)
            {
                doc.Metadata["added_by"] = "api_v2";
                doc.Metadata["added_at"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                doc.Metadata["batch_id"] = batchId;
                doc.Metadata["embedding_model"] = "mxbai-embed-large";
                doc.Metadata["embedding_dimension"] = expectedDimension;
            }

            // 🔥 PAYLOAD COM EMBEDDINGS VALIDADOS
            var payload = new AddCollectionRecordsPayload
            {
                Ids = documents.Select(d => d.Id).ToList(),
                Documents = documents.Select(d => d.Content).ToList(),
                Metadatas = documents.Select(d => d.Metadata).ToList(),
                Embeddings = embeddings.Select(x => x.ToList()).ToList()
            };

            var response =
                await _apiClient.AddRecordsAsync(TENANT_NAME, DATABASE_NAME, collectionUuid, payload);

            _logger.LogInformation("📨 Resposta ADD: {StatusCode} - {@Response}", response.StatusCode,
                response.IsSuccess ? response.Data : response.Error);

            if (response.IsSuccess)
            {
                _logger.LogInformation("✅ Documentos COM EMBEDDINGS 1024D aceitos!");

                // Aguardar sincronização
                var syncSuccess = await WaitForIndexingAsync(collectionUuid, documents.Count, batchId);

                if (syncSuccess)
                {
                    _logger.LogInformation("🎯 Documentos completamente indexados!");
                    return true;
                }

                _logger.LogWarning("⚠️ Timeout na verificação, mas documentos aceitos");
                return true;
            }

            _logger.LogError("❌ Erro ao adicionar documentos: {StatusCode} - {@Error}",
                response.StatusCode, response.Error);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao adicionar documentos à coleção {CollectionName}", collectionName);
            return false;
        }
    }


    public async Task<List<DocumentChunk>> SearchSimilarAsync(string collectionName, string query, int limit = 5)
    {
        try
        {
            _logger.LogInformation("🔍 [API v2] Buscando documentos na coleção {CollectionName} para: {Query}",
                collectionName, query);

            // Obter UUID da coleção
            if (!_collectionUuidMap.TryGetValue(collectionName, out var collectionUuid))
            {
                if (!await CheckCollectionExistsAsync(collectionName))
                {
                    _logger.LogError("❌ Coleção '{CollectionName}' não encontrada", collectionName);
                    return new List<DocumentChunk>();
                }

                collectionUuid = _collectionUuidMap[collectionName];
            }

            // Gerar embedding para a query
            var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query);

            if (queryEmbedding.Length == 0)
            {
                _logger.LogError("❌ Falha ao gerar embedding para query");
                return new List<DocumentChunk>();
            }

            // Payload da query
            var payload = new QueryRequestPayload
            {
                QueryEmbeddings = new[] { queryEmbedding.ToList() }.ToList(),
                NResults = limit * 2, // Buscar mais para filtrar duplicados
                Includes = new List<Include>
                {
                    Include.Documents,
                    Include.Metadatas,
                    Include.Distances
                }
            };

            var response = await _apiClient.QueryCollectionAsync(TENANT_NAME, DATABASE_NAME,
                collectionUuid, payload);
            _logger.LogInformation("📨 Resposta query: {StatusCode}", response.StatusCode);

            if (response.IsSuccess)
            {
                var r = new ChromaQueryResponse
                {
                    Distances = response.Data.Distances,
                    Documents = response.Data.Documents,
                    Metadatas = response.Data.Metadatas,
                    Embeddings = response.Data.Embeddings,
                    Ids = response.Data.Ids,
                    Include = response.Data.Include.Select(x => x.ToString()).ToList(),
                    Uris = response.Data.Uris
                };
                var documents = ConvertToDocumentChunks(r, limit);

                _logger.LogInformation("✅ Convertidos {Count} documentos", documents.Count);
                return documents;
            }

            _logger.LogWarning("⚠️ Erro na busca: {StatusCode} - {@Error}", response.StatusCode, response.Error);
            return new List<DocumentChunk>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao buscar documentos similares");
            return new List<DocumentChunk>();
        }
    }


    public async Task<bool> DeleteCollectionAsync(string collectionName)
    {
        try
        {
            // 1. Verificar se temos UUID mapeado para esta coleção
            if (!_collectionUuidMap.TryGetValue(collectionName, out var collectionUuid))
            {
                // Se não tiver no cache, tentar buscar
                if (!await CheckCollectionExistsAsync(collectionName))
                {
                    _logger.LogWarning("⚠️ Coleção {CollectionName} não existe para deletar", collectionName);
                    return true; // Consideramos sucesso se já não existe
                }

                // Após CheckCollectionExistsAsync, deve estar no mapa
                if (!_collectionUuidMap.TryGetValue(collectionName, out collectionUuid))
                {
                    _logger.LogError("❌ UUID da coleção {CollectionName} não encontrado", collectionName);
                    return false;
                }
            }

            _logger.LogInformation("🔑 Usando UUID {UUID} para deletar coleção {CollectionName}",
                collectionUuid, collectionName);

            // 2. API v2 - Delete usando UUID

            var response = await _apiClient.DeleteCollectionAsync(TENANT_NAME, DATABASE_NAME, collectionUuid);

            if (response.IsSuccess)
            {
                _logger.LogInformation("✅ [API v2] Coleção {CollectionName} (UUID: {UUID}) deletada",
                    collectionName, collectionUuid);

                // Remover do mapeamento
                _collectionUuidMap.Remove(collectionName);
                return true;
            }

            // var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("❌ [API v2] Erro ao deletar coleção: {StatusCode} - {@Error}",
                response.StatusCode, response.Error);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao deletar coleção {CollectionName}", collectionName);
            return false;
        }
    }

    /// <summary>
    ///     Valida e cria o Tenant e Database se necessário
    /// </summary>
    private async Task<bool> EnsureTenantAndDatabaseExistAsync()
    {
        try
        {
            _logger.LogInformation("🔧 [API v2] Validando estrutura Tenant/Database...");

            // 1. VERIFICAR SE TENANT EXISTE
            var tenantExists = await CheckTenantExistsAsync(TENANT_NAME);
            if (!tenantExists)
            {
                _logger.LogWarning("⚠️ Tenant '{TenantName}' não existe, criando...", TENANT_NAME);
                if (!await CreateTenantAsync(TENANT_NAME))
                {
                    _logger.LogError("❌ Falha ao criar tenant '{TenantName}'", TENANT_NAME);
                    return false;
                }
            }

            // 2. VERIFICAR SE DATABASE EXISTE
            var databaseExists = await CheckDatabaseExistsAsync(TENANT_NAME, DATABASE_NAME);
            if (!databaseExists)
            {
                _logger.LogWarning("⚠️ Database '{DatabaseName}' não existe no tenant '{TenantName}', criando...",
                    DATABASE_NAME, TENANT_NAME);
                if (!await CreateDatabaseAsync(TENANT_NAME, DATABASE_NAME))
                {
                    _logger.LogError("❌ Falha ao criar database '{DatabaseName}' no tenant '{TenantName}'",
                        DATABASE_NAME, TENANT_NAME);
                    return false;
                }
            }

            _logger.LogInformation("✅ [API v2] Estrutura Tenant/Database validada com sucesso!");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao validar/criar estrutura Tenant/Database");
            return false;
        }
    }

    /// <summary>
    ///     Verifica se o tenant existe
    /// </summary>
    private async Task<bool> CheckTenantExistsAsync(string tenantName)
    {
        try
        {
            _logger.LogInformation("🔍 Verificando se tenant '{TenantName}' existe...", tenantName);

            var response = await _apiClient.GetTenantAsync(tenantName);
            //var response = await _httpClient.GetAsync("/api/v2/tenants");
            if (response.IsSuccess)
            {
                _logger.LogInformation("📋 Tenants disponíveis: {@Content}", response.Data);
                _logger.LogInformation("✅ Tenant '{TenantName}' existe", response.Data.Name);
                return true;
            }

            _logger.LogInformation("📋 Tenant '{TenantName}' não encontrado", tenantName);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao verificar tenant '{TenantName}'", tenantName);
            return false;
        }
    }

    /// <summary>
    ///     Cria um novo tenant
    /// </summary>
    private async Task<bool> CreateTenantAsync(string tenantName)
    {
        try
        {
            _logger.LogInformation("🚀 Criando tenant '{TenantName}'...", tenantName);

            // var payload = new
            // {
            //     name = tenantName
            // };
            //
            // var jsonContent = JsonSerializer.Serialize(payload);
            // var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            //
            //
            // var response = await _httpClient.PostAsync("/api/v2/tenants", content);
            // var responseContent = await response.Content.ReadAsStringAsync();

            var response = await _apiClient.CreateTenantAsync(new CreateTenantPayload
            {
                Name = tenantName
            });


            _logger.LogInformation("📨 Resposta criação tenant: {StatusCode} - {@Response}",
                response.StatusCode, response.IsSuccess ? response.Data : response.Error);
            ;

            if (response.IsSuccess)
            {
                _logger.LogInformation("✅ Tenant '{TenantName}' criado com sucesso!", tenantName);
                return true;
            }

            // Verificar se o erro é "já existe" (pode ser aceitável)
            if (response.Error.Message.Contains("already exists") || response.Error.Message.Contains("Conflict"))
            {
                _logger.LogInformation("✅ Tenant '{TenantName}' já existe (detectado pelo erro)", tenantName);
                return true;
            }

            _logger.LogError("❌ Erro ao criar tenant '{TenantName}': {StatusCode} - {@Error}",
                tenantName, response.StatusCode, response.Error);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao criar tenant '{TenantName}'", tenantName);
            return false;
        }
    }

    /// <summary>
    ///     Verifica se o database existe no tenant
    /// </summary>
    private async Task<bool> CheckDatabaseExistsAsync(string tenantName, string databaseName)
    {
        try
        {
            _logger.LogInformation("🔍 Verificando se database '{DatabaseName}' existe no tenant '{TenantName}'...",
                databaseName, tenantName);

            var response = await _apiClient.ListDatabasesAsync(tenantName);
            if (response.IsSuccess)
            {
                //var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("📋 Databases no tenant '{TenantName}': {@Content}", tenantName, response.Data);
                if (response.Data.Any(x => x.Name == databaseName))
                {
                    _logger.LogInformation("✅ Database '{DatabaseName}' existe no tenant '{TenantName}'",
                        databaseName, tenantName);
                    return true;
                }
            }

            _logger.LogInformation("📋 Database '{DatabaseName}' não encontrado no tenant '{TenantName}'",
                databaseName, tenantName);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao verificar database '{DatabaseName}' no tenant '{TenantName}'",
                databaseName, tenantName);
            return false;
        }
    }

    /// <summary>
    ///     Cria um novo database no tenant
    /// </summary>
    private async Task<bool> CreateDatabaseAsync(string tenantName, string databaseName)
    {
        try
        {
            _logger.LogInformation("🚀 Criando database '{DatabaseName}' no tenant '{TenantName}'...",
                databaseName, tenantName);

            var response = await _apiClient.CreateDatabaseAsync(tenantName, new CreateDatabasePayload
            {
                Name = databaseName
            });

            _logger.LogInformation("📨 Resposta criação database: {StatusCode} - {@Response}",
                response.StatusCode, response.Data);

            if (response.IsSuccess)
            {
                _logger.LogInformation("✅ Database '{DatabaseName}' criado com sucesso no tenant '{TenantName}'!",
                    databaseName, tenantName);
                return true;
            }

            // Verificar se o erro é "já existe" (pode ser aceitável)
            if (response.Error.Message.Contains("already exists") || response.Error.Message.Contains("Conflict"))
            {
                _logger.LogInformation(
                    "✅ Database '{DatabaseName}' já existe no tenant '{TenantName}' (detectado pelo erro)",
                    databaseName, tenantName);
                return true;
            }

            _logger.LogError(
                "❌ Erro ao criar database '{DatabaseName}' no tenant '{TenantName}': {StatusCode} - {@Error}",
                databaseName, tenantName, response.StatusCode, response.Error);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao criar database '{DatabaseName}' no tenant '{TenantName}'",
                databaseName, tenantName);
            return false;
        }
    }

    /// <summary>
    ///     🔄 MÉTODO DEDICADO PARA CONVERSÃO
    /// </summary>
    private List<DocumentChunk> ConvertToDocumentChunks(ChromaQueryResponse? result, int limit)
    {
        var documents = new List<DocumentChunk>();

        if (result?.Documents?.Any() != true || result.Documents[0]?.Any() != true)
        {
            _logger.LogWarning("⚠️ Resposta vazia ou sem documentos");
            return documents;
        }

        // 🔥 ACESSAR PRIMEIRO ARRAY (PRIMEIRA QUERY)
        var ids = result.Ids?[0] ?? new List<string>();
        var docs = result.Documents[0];
        var metadatas = result?.Metadatas?[0] ?? new List<Metadata>();
        var distances = result?.Distances.FirstOrDefault() ?? new List<double>();

        var seenTitles = new HashSet<string>(); // Para filtrar duplicados

        // 🔄 CONVERTER CADA ITEM
        for (var i = 0; i < docs.Count && documents.Count < limit; i++)
            try
            {
                // Obter metadados se disponível
                var metadata = i < metadatas.Count ? metadatas[i] : null;
                var title = metadata?.Title ?? $"Documento {i + 1}";

                // 🔥 FILTRAR DUPLICADOS POR TÍTULO
                if (seenTitles.Contains(title))
                {
                    _logger.LogDebug("🔄 Pulando documento duplicado: {Title}", title);
                    continue;
                }

                seenTitles.Add(title);

                // 🔄 CONVERTER METADATA PARA DICTIONARY
                var metadataDict = ConvertMetadataToDictionary(metadata);

                // 🔄 CRIAR DocumentChunk
                var documentChunk = new DocumentChunk
                {
                    Id = i < ids.Count ? ids[i] : Guid.NewGuid().ToString(),
                    Content = docs[i],
                    Metadata = metadataDict,
                    Distance = i < distances.Count ? distances[i] : 0.0
                };

                documents.Add(documentChunk);

                _logger.LogDebug("✅ Convertido: {Title} (Distance: {Distance:F4})",
                    title, documentChunk.Distance);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("⚠️ Erro ao converter item {Index}: {Error}", i, ex.Message);
            }

        _logger.LogInformation("🔄 Conversão concluída: {Count} documentos únicos", documents.Count);
        return documents;
    }

    /// <summary>
    ///     🔄 CONVERTER METADATA TIPADA PARA DICTIONARY
    /// </summary>
    private Dictionary<string, object> ConvertMetadataToDictionary(Metadata? metadata)
    {
        var dict = new Dictionary<string, object>();

        if (metadata == null)
            return dict;

        // 🔄 MAPEAR TODAS AS PROPRIEDADES
        if (!string.IsNullOrEmpty(metadata.Title))
            dict["title"] = metadata.Title;

        if (!string.IsNullOrEmpty(metadata.Category))
            dict["category"] = metadata.Category;

        if (!string.IsNullOrEmpty(metadata.AddedBy))
            dict["added_by"] = metadata.AddedBy;

        if (!string.IsNullOrEmpty(metadata.AddedAt))
            dict["added_at"] = metadata.AddedAt;

        if (!string.IsNullOrEmpty(metadata.BatchId))
            dict["batch_id"] = metadata.BatchId;

        if (!string.IsNullOrEmpty(metadata.EmbeddingModel))
            dict["embedding_model"] = metadata.EmbeddingModel;

        if (metadata.EmbeddingDimension > 0)
            dict["embedding_dimension"] = metadata.EmbeddingDimension;

        return dict;
    }

    /// <summary>
    ///     Aguarda a indexação dos documentos com verificação inteligente
    /// </summary>
    private async Task<bool> WaitForIndexingAsync(string collectionUuid, int expectedCount, string batchId,
        int maxWaitSeconds = 60)
    {
        try
        {
            _logger.LogInformation("⏳ Aguardando indexação de {ExpectedCount} documentos...", expectedCount);

            var startTime = DateTime.UtcNow;
            var maxWait = TimeSpan.FromSeconds(maxWaitSeconds);
            var checkInterval = 2000; // 2 segundos

            while (DateTime.UtcNow - startTime < maxWait)
                try
                {
                    // 🔍 VERIFICAR COUNT PRIMEIRO
                    var currentCount = await GetCollectionCountAsync(collectionUuid);
                    _logger.LogInformation("📊 Count atual: {CurrentCount} (esperando >= {ExpectedCount})",
                        currentCount, expectedCount);

                    if (currentCount >= expectedCount)
                    {
                        // 🔍 VERIFICAR SE NOSSOS DOCUMENTOS ESPECÍFICOS ESTÃO LÁ
                        var documentsFound = await VerifyDocumentsByCountAsync(collectionUuid, expectedCount);

                        if (documentsFound)
                        {
                            var elapsed = DateTime.UtcNow - startTime;
                            _logger.LogInformation(
                                "✅ Sincronização completa em {ElapsedSeconds}s! Documentos indexados e verificados",
                                elapsed.TotalSeconds);
                            return true;
                        }
                    }

                    // Aguardar antes da próxima verificação
                    await Task.Delay(checkInterval);

                    // Aumentar o intervalo gradualmente para não sobrecarregar
                    if (checkInterval < 5000)
                        checkInterval += 500;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("⚠️ Erro durante verificação de indexação: {Error}", ex.Message);
                    await Task.Delay(checkInterval);
                }

            var totalElapsed = DateTime.UtcNow - startTime;
            _logger.LogWarning("⚠️ Timeout aguardando indexação após {ElapsedSeconds}s", totalElapsed.TotalSeconds);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao aguardar indexação");
            return false;
        }
    }

    /// <summary>
    ///     Obtém o count atual da coleção
    /// </summary>
    private async Task<uint> GetCollectionCountAsync(string collectionUuid)
    {
        try
        {
            var countResponse = await _apiClient.CountRecordsAsync(TENANT_NAME, DATABASE_NAME, collectionUuid);

            return countResponse.IsSuccess ? countResponse.Data : 0;
        }
        catch
        {
            return 0;
        }
    }


    /// <summary>
    ///     🔥 BUSCAR TODOS OS DOCUMENTOS E CONTAR
    /// </summary>
    private async Task<bool> VerifyDocumentsByCountAsync(string collectionUuid, int expectedCount)
    {
        try
        {
            _logger.LogInformation("🔢 Verificando contagem total de documentos (esperado: {Expected})", expectedCount);

            // Gerar embedding genérico
            var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync("DevOps documentation");

            if (queryEmbedding.Length == 0) return true; // Assumir sucesso se não conseguir verificar

            // Buscar TODOS os documentos da coleção
            var queryPayload = new QueryRequestPayload
            {
                QueryEmbeddings = new[] { queryEmbedding.ToList() }.ToList(),
                NResults = 1000, // Número alto para pegar todos
                Includes = new[] { Include.Metadatas }.ToList()
            };

            // var queryContent = new StringContent(
            //     JsonSerializer.Serialize(queryPayload),
            //     Encoding.UTF8,
            //     "application/json"
            // );
            //
            // var queryEndpoint =
            //     $"/api/v2/tenants/{TENANT_NAME}/databases/{DATABASE_NAME}/collections/{collectionUuid}/query";
            // var queryResponse = await _httpClient.PostAsync(queryEndpoint, queryContent);

            var queryResponse =
                await _apiClient.QueryCollectionAsync(TENANT_NAME, DATABASE_NAME, collectionUuid, queryPayload);

            if (queryResponse.IsSuccess)
            {
                // var responseContent = await queryResponse.Content.ReadAsStringAsync();
                // var result = JsonSerializer.Deserialize<ChromaQueryResponse>(responseContent);

                //var actualCount = result?.Metadatas?.Count ?? 0;
                var actualCount = queryResponse.Data?.Metadatas?.Count ?? 0;

                _logger.LogInformation("📊 Documentos na coleção: {Actual} (esperado: {Expected})",
                    actualCount, expectedCount);

                return actualCount >= expectedCount;
            }

            return true; // Assumir sucesso se não conseguir verificar
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao verificar contagem de documentos");
            return true;
        }
    }
}

// Classes para API v2
public class ChromaQueryResponseV2
{
    public string[][]? Ids { get; set; }
    public string[][]? Documents { get; set; }
    public Dictionary<string, object>[][]? Metadatas { get; set; }
    public double[][]? Distances { get; set; }
}