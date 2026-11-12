using System.Text.Json;
using AutoFix.Mvc.Models;
using AutoFix.Mvc.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoFix.Mvc.Controllers;

public class MovimientosController : Controller
{
    private readonly ApiClientService _api;

    public MovimientosController(ApiClientService api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(int? repuestoId = null)
    {
        if (!_api.EstaAutenticado) return RedirectToAction("Login", "Account");

        var url = repuestoId.HasValue ? $"api/movimientos?repuestoId={repuestoId.Value}" : "api/movimientos";
        var res = await _api.GetAsync<List<MovimientoVm>>(url);
        if (!res.Success)
        {
            TempData["Error"] = res.ErrorMessage;
            return View(new List<MovimientoVm>());
        }
        return View(res.Data ?? []);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? repuestoId = null, string? tipo = "Salida")
    {
        if (!_api.EstaAutenticado) return RedirectToAction("Login", "Account");
        await CargarListasAsync();
        return View(new MovimientoCrearVm
        {
            RepuestoId = repuestoId ?? 0,
            Tipo = tipo ?? "Salida"
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(MovimientoCrearVm model)
    {
        if (!ModelState.IsValid)
        {
            await CargarListasAsync();
            return View(model);
        }

        var endpoint = model.Tipo == "Entrada"
            ? $"api/inventario/{model.RepuestoId}/entrada"
            : $"api/inventario/{model.RepuestoId}/usar";

        var payload = new
        {
            cantidad = model.Cantidad,
            ordenReparacionId = model.OrdenReparacionId,
            observacion = model.Observacion,
            version = model.Version
        };

        var res = await _api.PostAsync<object, JsonElement>(endpoint, payload);
        if (!res.Success)
        {
            ModelState.AddModelError("", res.ErrorMessage ?? "Error al registrar movimiento.");
            await CargarListasAsync();
            return View(model);
        }

        var msg = res.Data.ValueKind == JsonValueKind.Object && res.Data.TryGetProperty("mensaje", out var m)
            ? m.GetString()
            : $"Movimiento de {model.Tipo} registrado correctamente.";

        TempData["Success"] = msg;
        return RedirectToAction(nameof(Index));
    }

    private async Task CargarListasAsync()
    {
        var repuestos = await _api.GetAsync<List<RepuestoVm>>("api/inventario");
        var ordenes = await _api.GetAsync<List<OrdenVm>>("api/ordenes");
        ViewBag.Repuestos = repuestos.Data ?? [];
        ViewBag.Ordenes = ordenes.Data ?? [];
    }
}
