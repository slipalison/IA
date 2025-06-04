namespace ResultPattern;

/// <summary>
///     Representa o resultado de uma operação que pode retornar dados ou erro
/// </summary>
/// <typeparam name="T">Tipo dos dados retornados em caso de sucesso</typeparam>
public class Result<T>
{
    private Result(bool isSuccess, T? data, ApiError? error, int statusCode)
    {
        IsSuccess = isSuccess;
        Data = data;
        Error = error;
        StatusCode = statusCode;
    }

    public bool IsSuccess { get; }
    public T? Data { get; }
    public ApiError? Error { get; }
    public int StatusCode { get; }

    public static Result<T> Success(T data, int statusCode = 200)
    {
        return new Result<T>(true, data, null, statusCode);
    }

    public static Result<T> Failure(ApiError error, int statusCode)
    {
        return new Result<T>(false, default, error, statusCode);
    }

    public static Result<T> Failure(string message, int statusCode, string? errorCode = null)
    {
        return new Result<T>(false, default, new ApiError(message, errorCode), statusCode);
    }
}

/// <summary>
///     Representa o resultado de uma operação que não retorna dados, apenas sucesso/erro
/// </summary>
public class Result
{
    private Result(bool isSuccess, ApiError? error, int statusCode)
    {
        IsSuccess = isSuccess;
        Error = error;
        StatusCode = statusCode;
    }

    public bool IsSuccess { get; }
    public ApiError? Error { get; }
    public int StatusCode { get; }

    public static Result Success(int statusCode = 200)
    {
        return new Result(true, null, statusCode);
    }

    public static Result Failure(ApiError error, int statusCode)
    {
        return new Result(false, error, statusCode);
    }

    public static Result Failure(string message, int statusCode, string? errorCode = null)
    {
        return new Result(false, new ApiError(message, errorCode), statusCode);
    }
}