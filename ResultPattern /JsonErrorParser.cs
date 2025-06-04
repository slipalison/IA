using System.Text.Json;

namespace ResultPattern;

/// <summary>
///     Utilitário para extrair informações de erro de objetos JSON genéricos
/// </summary>
internal static class JsonErrorParser
{
    private static readonly string[] MessageFields =
        { "message", "error", "detail", "description", "msg", "error_description" };

    private static readonly string[] CodeFields =
        { "code", "error_code", "type", "errorCode", "status" };

    public static string? ExtractErrorMessage(JsonElement root)
    {
        return ExtractStringProperty(root, MessageFields);
    }

    public static string? ExtractErrorCode(JsonElement root)
    {
        return ExtractProperty(root, CodeFields);
    }

    private static string? ExtractStringProperty(JsonElement root, string[] fieldNames)
    {
        foreach (var field in fieldNames)
        {
            if (!root.TryGetProperty(field, out var property)) continue;
            if (property.ValueKind != JsonValueKind.String) continue;

            var value = property.GetString();
            if (!string.IsNullOrEmpty(value)) return value;
        }

        return null;
    }

    private static string? ExtractProperty(JsonElement root, string[] fieldNames)
    {
        foreach (var field in fieldNames)
        {
            if (!root.TryGetProperty(field, out var property)) continue;

            return property.ValueKind switch
            {
                JsonValueKind.String => property.GetString(),
                JsonValueKind.Number => property.GetInt32().ToString(),
                _ => null
            };
        }

        return null;
    }
}