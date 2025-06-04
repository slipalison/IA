using System.Text.Json;
using Flurl.Http;

namespace ResultPattern;

/// <summary>
///     Extensões para encapsular responses HTTP usando o padrão Result
/// </summary>
public static class FlurlResultExtensions
{
    public static async Task<Result<T>> GetJsonResultAsync<T>(this IFlurlRequest request)
    {
        return await ExecuteWithExceptionHandling(async () => await ProcessResponse<T>(await request.GetAsync()));
    }

    public static async Task<Result> GetResultAsync(this IFlurlRequest request)
    {
        return await ExecuteWithExceptionHandling(async () => await ProcessResponse(await request.GetAsync()));
    }

    public static async Task<Result<T>> PostJsonResultAsync<T>(this IFlurlRequest request, object? data = null)
    {
        return await ExecuteWithExceptionHandling(async () =>
            await ProcessResponse<T>(await request.PostJsonAsync(data)));
    }

    public static async Task<Result<T>> PutJsonResultAsync<T>(this IFlurlRequest request, object? data = null)
    {
        return await ExecuteWithExceptionHandling(async () =>
            await ProcessResponse<T>(await request.PutJsonAsync(data)));
    }

    public static async Task<Result> DeleteResultAsync(this IFlurlRequest request)
    {
        return await ExecuteWithExceptionHandling(async () => await ProcessVoidResponse(await request.DeleteAsync()));
    }

    public static async Task<Result<string>> GetStringResultAsync(this IFlurlRequest request)
    {
        return await ExecuteWithExceptionHandling(async () => await ProcessStringResponse(await request.GetAsync()));
    }


    private static async Task<Result<T>> ExecuteWithExceptionHandling<T>(Func<Task<Result<T>>> operation)
    {
        try
        {
            return await operation();
        }
        catch (FlurlHttpException ex)
        {
            return await HandleFlurlException<T>(ex);
        }
        catch (Exception ex)
        {
            return Result<T>.Failure(ApiError.FromException(ex), 500);
        }
    }

    private static async Task<Result> ExecuteWithExceptionHandling(Func<Task<Result>> operation)
    {
        try
        {
            return await operation();
        }
        catch (FlurlHttpException ex)
        {
            return await HandleFlurlException(ex);
        }
        catch (Exception ex)
        {
            return Result.Failure(ApiError.FromException(ex), 500);
        }
    }

    private static async Task<Result<T>> ProcessResponse<T>(IFlurlResponse response)
    {
        var statusCode = response.StatusCode;

        if (!response.ResponseMessage.IsSuccessStatusCode)
        {
            var error = await CreateErrorFromResponse(response);
            return Result<T>.Failure(error, statusCode);
        }

        var data = await DeserializeResponseData<T>(response);
        return Result<T>.Success(data, statusCode);
    }

    private static async Task<Result> ProcessResponse(IFlurlResponse response)
    {
        var statusCode = response.StatusCode;

        if (response.ResponseMessage.IsSuccessStatusCode) return Result.Success(statusCode);
        var error = await CreateErrorFromResponse(response);
        return Result.Failure(error, statusCode);
    }

    private static async Task<Result> ProcessVoidResponse(IFlurlResponse response)
    {
        var statusCode = response.StatusCode;

        if (response.ResponseMessage.IsSuccessStatusCode) return Result.Success(statusCode);

        var error = await CreateErrorFromResponse(response);
        return Result.Failure(error, statusCode);
    }

    private static async Task<Result<string>> ProcessStringResponse(IFlurlResponse response)
    {
        var statusCode = response.StatusCode;

        if (response.ResponseMessage.IsSuccessStatusCode)
        {
            var content = await response.GetStringAsync();
            return Result<string>.Success(content, statusCode);
        }

        var errorContent = await response.GetStringAsync();
        var error = ApiError.FromHttpError(statusCode, errorContent);
        return Result<string>.Failure(error, statusCode);
    }

    private static async Task<T> DeserializeResponseData<T>(IFlurlResponse response)
    {
        if (typeof(T) == typeof(string))
        {
            var stringContent = await response.GetStringAsync();
            return (T)(object)stringContent;
        }

        if (IsPrimitiveType<T>())
        {
            var content = await response.GetStringAsync();
            var converted = Convert.ChangeType(content.Trim('"'), typeof(T));
            return (T)converted;
        }

        return await response.GetJsonAsync<T>();
    }


    private static bool IsPrimitiveType<T>()
    {
        return typeof(T).IsPrimitive || typeof(T) == typeof(bool);
    }

    private static async Task<ApiError> CreateErrorFromResponse(IFlurlResponse response)
    {
        var errorContent = await GetResponseContent(response);
        var statusCode = response.StatusCode;
        var contentType = GetContentType(response);

        if (string.IsNullOrEmpty(errorContent)) return ApiError.FromHttpError(statusCode, "Resposta sem conteúdo");

        var structuredError = await TryParseStructuredError(errorContent, contentType);
        return structuredError ?? new RawApiError(errorContent, statusCode, contentType);
    }

    private static async Task<string> GetResponseContent(IFlurlResponse response)
    {
        try
        {
            return await response.GetStringAsync();
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string? GetContentType(IFlurlResponse response)
    {
        return response.ResponseMessage.Content?.Headers?.ContentType?.MediaType;
    }

    private static async Task<ApiError?> TryParseStructuredError(string errorContent, string? contentType)
    {
        if (!IsJsonContent(errorContent)) return null;

        var errorResponseResult = await TryParseAsErrorResponse(errorContent);
        if (errorResponseResult != null) return errorResponseResult;

        return await TryParseAsGenericJsonError(errorContent, contentType);
    }

    private static bool IsJsonContent(string content)
    {
        return content.TrimStart().StartsWith("{");
    }

    private static async Task<ApiError?> TryParseAsErrorResponse(string errorContent)
    {
        return await Task.Run(() =>
        {
            try
            {
                var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(errorContent, GetJsonOptions());
                return errorResponse != null && !string.IsNullOrEmpty(errorResponse.Message)
                    ? ApiError.FromErrorResponse(errorResponse)
                    : null;
            }
            catch (JsonException)
            {
                return null;
            }
        });
    }

    private static async Task<ApiError?> TryParseAsGenericJsonError(string errorContent, string? contentType)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var document = JsonDocument.Parse(errorContent);
                var root = document.RootElement;

                var message = JsonErrorParser.ExtractErrorMessage(root);
                var code = JsonErrorParser.ExtractErrorCode(root);

                if (string.IsNullOrEmpty(message)) return null;

                return new ApiError(message, code, CreateErrorDetails(errorContent, contentType, "generic_json"));
            }
            catch (JsonException)
            {
                return null;
            }
        });
    }

    private static Dictionary<string, object> CreateErrorDetails(string rawContent, string? contentType,
        string source)
    {
        return new Dictionary<string, object>
        {
            { "raw_content", rawContent },
            { "content_type", contentType ?? "application/json" },
            { "parsed_from", source }
        };
    }

    private static JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    private static async Task<Result<T>> HandleFlurlException<T>(FlurlHttpException ex)
    {
        var statusCode = ex.StatusCode ?? 500;
        var errorContent = await GetFlurlExceptionContent(ex);
        var contentType = GetFlurlExceptionContentType(ex);

        if (string.IsNullOrEmpty(errorContent)) return CreateFlurlExceptionResult<T>(ex, statusCode);

        var structuredError = await TryParseStructuredError(errorContent, contentType);
        if (structuredError != null) return Result<T>.Failure(structuredError, statusCode);

        var rawError = new RawApiError(errorContent, statusCode, contentType);
        return Result<T>.Failure(rawError, statusCode);
    }

    private static async Task<Result> HandleFlurlException(FlurlHttpException ex)
    {
        var statusCode = ex.StatusCode ?? 500;
        var errorContent = await GetFlurlExceptionContent(ex);
        var contentType = GetFlurlExceptionContentType(ex);

        if (string.IsNullOrEmpty(errorContent)) return CreateFlurlExceptionResult(ex, statusCode);

        var structuredError = await TryParseStructuredError(errorContent, contentType);
        if (structuredError != null) return Result.Failure(structuredError, statusCode);

        var rawError = new RawApiError(errorContent, statusCode, contentType);
        return Result.Failure(rawError, statusCode);
    }

    private static async Task<string> GetFlurlExceptionContent(FlurlHttpException ex)
    {
        try
        {
            return ex.Message != null ? await ex.GetResponseStringAsync() : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string? GetFlurlExceptionContentType(FlurlHttpException ex)
    {
        if (ex.Call?.HttpResponseMessage?.Headers?.TryGetValues("Content-Type", out var contentType) ?? false)
            return contentType.FirstOrDefault();
        return string.Empty;
    }

    private static Result<T> CreateFlurlExceptionResult<T>(FlurlHttpException ex, int statusCode)
    {
        var error = new ApiError(ex.Message, "FLURL_EXCEPTION", new Dictionary<string, object>
        {
            { "exception_type", ex.GetType().Name },
            { "has_response", ex.Message != null }
        });

        return Result<T>.Failure(error, statusCode);
    }

    private static Result CreateFlurlExceptionResult(FlurlHttpException ex, int statusCode)
    {
        var error = new ApiError(ex.Message, "FLURL_EXCEPTION", new Dictionary<string, object>
        {
            { "exception_type", ex.GetType().Name },
            { "has_response", ex.Message != null }
        });

        return Result.Failure(error, statusCode);
    }
}