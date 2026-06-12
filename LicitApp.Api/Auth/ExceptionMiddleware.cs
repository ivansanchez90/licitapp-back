using LicitApp.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LicitApp.Api.Auth;

/// <summary>Traduce AppException (y errores no controlados) a ProblemDetails con el código correcto.</summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (AppException ex)
        {
            await WriteProblem(ctx, ex.StatusCode, ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            await WriteProblem(ctx, StatusCodes.Status401Unauthorized, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error no controlado");
            await WriteProblem(ctx, StatusCodes.Status500InternalServerError, "Error interno del servidor.");
        }
    }

    private static async Task WriteProblem(HttpContext ctx, int status, string detail)
    {
        if (ctx.Response.HasStarted) return;
        ctx.Response.StatusCode = status;
        var problem = new ProblemDetails { Status = status, Title = detail, Detail = detail };
        await ctx.Response.WriteAsJsonAsync(problem);
    }
}
