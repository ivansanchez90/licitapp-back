namespace LicitApp.Api.Services;

/// <summary>
/// Error de negocio con código HTTP. Lo traduce un middleware a ProblemDetails.
/// 403 = rol incorrecto, 404 = no existe, 409 = conflicto de estado.
/// </summary>
public class AppException : Exception
{
    public int StatusCode { get; }

    public AppException(int statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }

    public static AppException NotFound(string message) => new(404, message);
    public static AppException Forbidden(string message) => new(403, message);
    public static AppException Conflict(string message) => new(409, message);
    public static AppException BadRequest(string message) => new(400, message);
}
