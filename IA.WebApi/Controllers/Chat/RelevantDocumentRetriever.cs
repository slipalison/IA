using IA.WebApi.Services;

namespace IA.WebApi.Controllers.Chat;

public class RelevantDocumentRetriever : IRelevantDocumentRetriever
{
    private const string COLLECTION_NAME = "devops-docs";

    private readonly IChromaService _chromaService;
    private readonly ILogger<RelevantDocumentRetriever> _logger;

    public RelevantDocumentRetriever(IChromaService chromaService, ILogger<RelevantDocumentRetriever> logger)
    {
        _chromaService = chromaService;
        _logger = logger;
    }

    public async Task<List<DocumentChunk>> RetrieveRelevantDocumentsAsync(string query, int maxDocuments = 5)
    {
        try
        {
            _logger.LogInformation("Buscando documentos relevantes para: {Query}", query);

            var documents = await _chromaService.SearchSimilarAsync(COLLECTION_NAME, query, maxDocuments);

            _logger.LogInformation("Encontrados {Count} documentos relevantes", documents.Count);
            return documents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar documentos relevantes");
            return new List<DocumentChunk>();
        }
    }
}