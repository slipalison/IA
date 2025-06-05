namespace IA.WebApi.Controllers.Chat;

public class ChatMetadata
{
    public string ModelUsed { get; set; } = string.Empty;
    public double ProcessingTimeMs { get; set; }
    public int DocumentsFound { get; set; }
    public bool UsedRAG { get; set; }
    public int TokensGenerated { get; set; }
    public string? RequestId { get; set; } = Guid.NewGuid().ToString();
}