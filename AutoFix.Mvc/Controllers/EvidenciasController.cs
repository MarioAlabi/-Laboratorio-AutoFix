using AutoFix.Mvc.Models;
using AutoFix.Mvc.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoFix.Mvc.Controllers;

public class EvidenciasController : Controller
{
    private readonly ApiClientService _api;

    public EvidenciasController(ApiClientService api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(int? ordenId = null)
    {
        if (!_api.EstaAutenticado) return RedirectToAction("Login", "Account");

        var url = ordenId.HasValue ? $"api/evidencias?ordenId={ordenId.Value}" : "api/evidencias";
        var res = await _api.GetAsync<List<EvidenciaVm>>(url);
        var ordenes = await _api.GetAsync<List<OrdenVm>>("api/ordenes");
        ViewBag.Ordenes = ordenes.Data ?? [];
        ViewBag.OrdenIdFiltro = ordenId;

        if (!res.Success)
        {
            TempData["Error"] = res.ErrorMessage;
            return View(new List<EvidenciaVm>());
        }
        return View(res.Data ?? []);
    }

    [HttpGet]
    public async Task<IActionResult> Upload(int? ordenId = null)
    {
        if (!_api.EstaAutenticado) return RedirectToAction("Login", "Account");

        var ordenes = await _api.GetAsync<List<OrdenVm>>("api/ordenes");
        ViewBag.Ordenes = ordenes.Data ?? [];
        ViewBag.OrdenId = ordenId;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Upload(int ordenId, string categoria, string? descripcion, IFormFile? archivo)
    {
        if (archivo is null || archivo.Length == 0)
        {
            TempData["Error"] = "Debe seleccionar un archivo JPG, JPEG o PNG.";
            return RedirectToAction(nameof(Upload), new { ordenId });
        }

        var fields = new Dictionary<string, string?>
        {
            ["categoria"] = categoria,
            ["descripcion"] = descripcion
        };

        var res = await _api.PostFileAsync<EvidenciaVm>($"api/ordenes/{ordenId}/evidencias", archivo, fields);
        if (!res.Success)
        {
            TempData["Error"] = res.ErrorMessage;
            return RedirectToAction(nameof(Upload), new { ordenId });
        }

        TempData["Success"] = "Evidencia fotográfica guardada correctamente.";
        return RedirectToAction("Details", "Ordenes", new { id = ordenId });
    }
}
