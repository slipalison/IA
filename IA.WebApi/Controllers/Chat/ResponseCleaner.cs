namespace IA.WebApi.Controllers.Chat;

public class ResponseCleaner : IResponseCleaner
{
    private readonly string[] _prefixesToRemove = { "DevOpsGPT:", "Assistente:", "AI:" };

    public string CleanResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return "Desculpe, não consegui gerar uma resposta.";

        var cleaned = RemovePrefixes(response);
        cleaned = TrimWhitespace(cleaned);
        cleaned = EnsureProperEnding(cleaned);

        return cleaned;
    }

    private string RemovePrefixes(string response)
    {
        var result = response;

        foreach (var prefix in _prefixesToRemove)
            if (result.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                result = result.Substring(prefix.Length);
                break;
            }

        return result;
    }

    private static string TrimWhitespace(string text)
    {
        return text.Trim();
    }

    private static string EnsureProperEnding(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var lastChar = text[text.Length - 1];
        var hasPunctuation = lastChar == '.' || lastChar == '!' || lastChar == '?';

        return hasPunctuation ? text : text + ".";
    }
}