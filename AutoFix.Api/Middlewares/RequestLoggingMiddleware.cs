using System.Diagnostics;
using AutoFix.Api.Data;
using AutoFix.Api.Models;

namespace AutoFix.Api.Middlewares;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        IServiceScopeFactory scopeFactory,
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        Exception? excepcionCapturada = null;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            excepcionCapturada = ex;
            throw;
        }
        finally
        {
            sw.Stop();

            if (context.Request.Path.StartsWithSegments("/api"))
            {
                var correlationId = context.Items["CorrelationId"]?.ToString() ?? string.Empty;
                var usuario = context.User.Identity?.IsAuthenticated == true
                    ? context.User.Identity.Name ?? "Autenticado"
                    : "Anonimo";
                var metodo = context.Request.Method;
                var endpoint = $"{context.Request.Path}{context.Request.QueryString}";

                int codigoHttp;
                string? detalleExcepcion = null;

                if (excepcionCapturada is not null)
                {
                    var mapped = ExceptionHandlingMiddleware.MapearExcepcion(excepcionCapturada);
                    codigoHttp = mapped.StatusCode;
                    detalleExcepcion = $"{excepcionCapturada.GetType().Name}: {excepcionCapturada.Message}";
                }
                else
                {
                    codigoHttp = context.Response.StatusCode;
                    if (codigoHttp >= 400)
                        detalleExcepcion = $"Respuesta HTTP {codigoHttp}";
                }

                var fechaHora = DateTime.Now;

                _logger.LogInformation(
                    "HTTP {Metodo} {Endpoint} | Usuario={Usuario} | Codigo={Codigo} | Duracion={Duracion}ms | CorrelationId={CorrelationId} | Error={Error}",
                    metodo, endpoint, usuario, codigoHttp, sw.ElapsedMilliseconds, correlationId, detalleExcepcion ?? "Ninguno");

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AutoFixDbContext>();
                    db.LogsRegistro.Add(new LogRegistro
                    {
                        Usuario = usuario,
                        MetodoHttp = metodo,
                        Endpoint = endpoint,
                        FechaHora = fechaHora,
                        CodigoHttp = codigoHttp,
                        DuracionMs = sw.ElapsedMilliseconds,
                        CorrelationId = correlationId,
                        Excepcion = detalleExcepcion
                    });
                    await db.SaveChangesAsync();
                }
                catch (Exception logEx)
                {
                    _logger.LogError(logEx, "No se pudo guardar el log en SQLite [{CorrelationId}]", correlationId);
                }
            }
        }
    }
}
