namespace ResultPattern;

/// <summary>
///     Representa um erro de API com informações estruturadas
/// </summary>
public class ApiError
{
    private const string DefaultErrorMessage = "Erro desconhecido";
    private const string InternalErrorCode = "INTERNAL_ERROR";

    public ApiError(string message, string? errorCode = null, Dictionary<string, object>? details = null)
    {
        Message = string.IsNullOrWhiteSpace(message) ? DefaultErrorMessage : message;
        ErrorCode = errorCode;
        Details = details;
    }

    public string Message { get; protected set; }
    public string? ErrorCode { get; }
    public Dictionary<string, object>? Details { get; }

    public static ApiError FromErrorResponse(ErrorResponse errorResponse)
    {
        return new ApiError(errorResponse.Message, errorResponse.Error);
    }

    public static ApiError FromException(Exception exception)
    {
        return new ApiError($"Erro interno: {exception.Message}", InternalErrorCode);
    }

    public static ApiError FromHttpError(int statusCode, string? content = null)
    {
        return new ApiError($"Erro HTTP {statusCode}: {content ?? DefaultErrorMessage}", $"HTTP_{statusCode}");
    }
}