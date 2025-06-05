namespace IA.WebApi.Controllers.Chat;

public class SourceDocument
{
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Excerpt { get; set; } = string.Empty;
    public double Relevance { get; set; }
    public string? Url { get; set; }
    public DateTime? LastUpdated { get; set; }
}