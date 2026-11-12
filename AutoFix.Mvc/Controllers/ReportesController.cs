using AutoFix.Mvc.Models;
using AutoFix.Mvc.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoFix.Mvc.Controllers;

public class ReportesController : Controller
{
    private readonly ApiClientService _api;

    public ReportesController(ApiClientService api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(int? codigoHttp = null, string? usuario = null, string? correlationId = null)
    {
        if (!_api.EstaAutenticado) return RedirectToAction("Login", "Account");

        var opRes = await _api.GetAsync<ReporteOperacionesVm>("api/reportes/operaciones");
        if (!opRes.Success)
        {
            TempData["Error"] = opRes.ErrorMessage;
            return View(new ReporteOperacionesVm());
        }

        var queryParams = new List<string>();
        if (codigoHttp.HasValue) queryParams.Add($"codigoHttp={codigoHttp.Value}");
        if (!string.IsNullOrWhiteSpace(usuario)) queryParams.Add($"usuario={Uri.EscapeDataString(usuario)}");
        if (!string.IsNullOrWhiteSpace(correlationId)) queryParams.Add($"correlationId={Uri.EscapeDataString(correlationId)}");

        var logsUrl = "api/reportes/logs" + (queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "");
        var logsRes = await _api.GetAsync<List<LogRegistroVm>>(logsUrl);

        ViewBag.Logs = logsRes.Data ?? [];
        ViewBag.CodigoHttp = codigoHttp;
        ViewBag.Usuario = usuario;
        ViewBag.CorrelationId = correlationId;

        return View(opRes.Data ?? new ReporteOperacionesVm());
    }
}
