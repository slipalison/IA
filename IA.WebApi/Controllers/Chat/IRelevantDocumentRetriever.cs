using IA.WebApi.Services;

namespace IA.WebApi.Controllers.Chat;

public interface IRelevantDocumentRetriever
{
    Task<List<DocumentChunk>> RetrieveRelevantDocumentsAsync(string query, int maxDocuments = 5);
}