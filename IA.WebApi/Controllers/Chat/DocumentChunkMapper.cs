using IA.WebApi.Services;

namespace IA.WebApi.Controllers.Chat;

public static class DocumentChunkMapper
{
    public static SourceDocument ToSourceDocument(DocumentChunk chunk)
    {
        return new SourceDocument
        {
            Title = ExtractTitle(chunk),
            Category = ExtractCategory(chunk),
            Excerpt = TruncateContent(chunk.Content, 150),
            Relevance = CalculateRelevance(chunk.Distance ?? 0)
        };
    }

    private static string ExtractTitle(DocumentChunk chunk)
    {
        return chunk.Metadata.GetValueOrDefault("title", "Documento")?.ToString() ?? "Documento";
    }

    private static string ExtractCategory(DocumentChunk chunk)
    {
        return chunk.Metadata.GetValueOrDefault("category", "geral")?.ToString() ?? "geral";
    }

    private static string TruncateContent(string content, int maxLength)
    {
        if (string.IsNullOrEmpty(content) || content.Length <= maxLength)
            return content;

        return content.Substring(0, maxLength) + "...";
    }

    private static double CalculateRelevance(double distance)
    {
        return Math.Round((1.0 - distance) * 100, 1);
    }
}