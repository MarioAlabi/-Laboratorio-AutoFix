using System.Text.Json;
using AutoFix.Api.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AutoFix.Api.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public static (int StatusCode, string Mensaje) MapearExcepcion(Exception ex) => ex switch
    {
        NotFoundException => (StatusCodes.Status404NotFound, ex.Message),
        BusinessException => (StatusCodes.Status400BadRequest, ex.Message),
        ConflictException => (StatusCodes.Status409Conflict, ex.Message),
        DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Conflicto de concurrencia: otro usuario modificó el registro."),
        ForbiddenException => (StatusCodes.Status403Forbidden, ex.Message),
        UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "No autorizado."),
        _ => (StatusCodes.Status500InternalServerError, "Ocurrió un error interno al procesar la solicitud.")
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var correlationId = context.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();
            var (statusCode, mensaje) = MapearExcepcion(ex);

            if (statusCode == StatusCodes.Status500InternalServerError)
                _logger.LogError(ex, "Error no controlado [{CorrelationId}] en {Method} {Path}", correlationId, context.Request.Method, context.Request.Path);
            else
                _logger.LogWarning("Error controlado {StatusCode} [{CorrelationId}] en {Method} {Path}: {Message}", statusCode, correlationId, context.Request.Method, context.Request.Path, ex.Message);

            if (!context.Response.HasStarted)
            {
                context.Response.Clear();
                context.Response.StatusCode = statusCode;
                context.Response.ContentType = "application/json";
                context.Response.Headers["X-Correlation-ID"] = correlationId;

                var payload = JsonSerializer.Serialize(new
                {
                    mensaje,
                    codigoHttp = statusCode,
                    correlationId
                });
                await context.Response.WriteAsync(payload);
            }
        }
    }
}
