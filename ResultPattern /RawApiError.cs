using System.Text.RegularExpressions;

namespace ResultPattern;

/// <summary>
///     Representa um erro com conteúdo raw que não pôde ser deserializado
/// </summary>
public class RawApiError : ApiError
{
    private const int MaxContentLength = 500;
    private const int MaxPlainTextLength = 200;
    private const string TruncationSuffix = "...";

    public RawApiError(string rawContent, int statusCode, string? contentType = null)
        : base($"Erro HTTP {statusCode}", $"HTTP_{statusCode}")
    {
        RawContent = rawContent ?? string.Empty;
        ContentType = contentType ?? "unknown";
        IsJson = IsValidJsonContent(rawContent);

        var extractedMessage = ExtractMeaningfulMessage(rawContent);
        if (!string.IsNullOrEmpty(extractedMessage)) Message = extractedMessage;
    }

    public string RawContent { get; }
    public string ContentType { get; }
    public bool IsJson { get; }

    private static bool IsValidJsonContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return false;

        var trimmed = content.Trim();
        return (trimmed.StartsWith("{") && trimmed.EndsWith("}")) ||
               (trimmed.StartsWith("[") && trimmed.EndsWith("]"));
    }

    private static string ExtractMeaningfulMessage(string rawContent)
    {
        if (string.IsNullOrWhiteSpace(rawContent))
            return "Erro sem conteúdo";

        if (ShouldTruncateContent(rawContent)) return TruncateContent(rawContent);

        var titleMessage = TryExtractHtmlTitle(rawContent);
        if (!string.IsNullOrEmpty(titleMessage)) return titleMessage;

        var plainText = TryExtractPlainTextFromHtml(rawContent);
        if (!string.IsNullOrEmpty(plainText)) return plainText;

        return rawContent;
    }

    private static bool ShouldTruncateContent(string content)
    {
        return content.Length > MaxContentLength;
    }

    private static string TruncateContent(string content)
    {
        return content.Substring(0, MaxContentLength - TruncationSuffix.Length) + TruncationSuffix;
    }

    private static string? TryExtractHtmlTitle(string content)
    {
        if (!content.Contains("<title>")) return null;

        var titleMatch = Regex.Match(content, @"<title>(.*?)</title>", RegexOptions.IgnoreCase);
        return titleMatch.Success ? $"Erro: {titleMatch.Groups[1].Value}" : null;
    }

    private static string? TryExtractPlainTextFromHtml(string content)
    {
        if (!IsHtmlContent(content)) return null;

        var plainText = Regex.Replace(content, @"<[^>]+>", "").Trim();

        return IsValidPlainText(plainText) ? plainText : null;
    }

    private static bool IsHtmlContent(string content)
    {
        return content.Contains("<") && content.Contains(">");
    }

    private static bool IsValidPlainText(string plainText)
    {
        return !string.IsNullOrEmpty(plainText) && plainText.Length < MaxPlainTextLength;
    }
}