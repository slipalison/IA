using ChromaDb.Responses;

namespace IA.WebApi.Services;

public class DocumentData
{
    public List<string> Ids { get; set; } = new();
    public List<string> Documents { get; set; } = new();
    public List<Metadata> Metadatas { get; set; } = new();
    public List<double> Distances { get; set; } = new();
}