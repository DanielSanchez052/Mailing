using System.Text.Json;

namespace Mailing.Lambda.Core.Types;

public static class ErrorCodes
{
    public const string InvalidRequest = "INVALID_REQUEST";
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string InternalServerError = "INTERNAL_SERVER_ERROR";
    public const string NotFound = "NOT_FOUND";
}

public class Response<T>
{
    public Response()
    {
    }

    internal Response(T? data, bool isSucess, Dictionary<string, string>? errors = null, string? errorType = null)
    {
        Data = data;
        IsSucess = isSucess;
        Errors = errors ?? new Dictionary<string, string>();
        ErrorType = errorType ?? "";
    }

    public T? Data { get; set; }
    public bool IsSucess { get; set; } = false;
    public Dictionary<string, string> Errors { get; set; } = new Dictionary<string, string>();
    public string? ErrorType { private get; set; }

    public static Response<T> Success(T data)
    {
        return new Response<T>(data, true);
    }

    public static Response<T> ValidationError(T data, Dictionary<string, string> errors)
    {
        return new Response<T>(data, false, errors, ErrorCodes.ValidationFailed);
    }

    public static Response<T> NotFoundError(T data, Dictionary<string, string>? errors)
    {
        return new Response<T>(data, false, errors, ErrorCodes.NotFound);
    }

    public static Response<T> InternalServerError()
    {
        Dictionary<string, string> errors = new(){
            { "Message", "Ha ocurrido un error inesperado, intentalo de nuevo mas tarde."}
        };

        return new Response<T>(default, false, errors, ErrorCodes.InternalServerError);
    }

    public static Response<T> InvalidRequestError()
    {
        Dictionary<string, string> errors = new(){
            { "Message", "El Request enviado es invalido, por favor intentalo nuevamente."}
        };

        return new Response<T>(default, false, errors, ErrorCodes.InternalServerError);
    }
}
