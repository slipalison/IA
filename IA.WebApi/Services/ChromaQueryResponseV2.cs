namespace IA.WebApi.Services;

public class ChromaQueryResponseV2
{
    public string[][]? Ids { get; set; }
    public string[][]? Documents { get; set; }
    public Dictionary<string, object>[][]? Metadatas { get; set; }
    public double[][]? Distances { get; set; }
}