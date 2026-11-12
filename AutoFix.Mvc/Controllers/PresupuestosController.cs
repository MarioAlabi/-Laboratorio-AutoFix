using AutoFix.Mvc.Models;
using AutoFix.Mvc.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoFix.Mvc.Controllers;

public class PresupuestosController : Controller
{
    private readonly ApiClientService _api;

    public PresupuestosController(ApiClientService api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index()
    {
        if (!_api.EstaAutenticado) return RedirectToAction("Login", "Account");

        var res = await _api.GetAsync<List<PresupuestoVm>>("api/presupuestos");
        if (!res.Success)
        {
            TempData["Error"] = res.ErrorMessage;
            return View(new List<PresupuestoVm>());
        }
        return View(res.Data ?? []);
    }

    public async Task<IActionResult> Details(int id)
    {
        if (!_api.EstaAutenticado) return RedirectToAction("Login", "Account");

        var res = await _api.GetAsync<PresupuestoVm>($"api/presupuestos/{id}");
        if (!res.Success || res.Data is null)
        {
            TempData["Error"] = res.ErrorMessage ?? "Presupuesto no encontrado.";
            return RedirectToAction(nameof(Index));
        }
        return View(res.Data);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? ordenId = null)
    {
        if (!_api.EstaAutenticado) return RedirectToAction("Login", "Account");
        await CargarOrdenesYRepuestosAsync();
        return View(new PresupuestoCreateVm { OrdenReparacionId = ordenId ?? 0 });
    }

    [HttpPost]
    public async Task<IActionResult> Create(PresupuestoCreateVm model, int[]? repuestoIds, int[]? cantidades)
    {
        if (repuestoIds is not null && cantidades is not null)
        {
            for (var i = 0; i < Math.Min(repuestoIds.Length, cantidades.Length); i++)
            {
                if (repuestoIds[i] > 0 && cantidades[i] > 0)
                {
                    model.Repuestos.Add(new PresupuestoItemVm
                    {
                        RepuestoId = repuestoIds[i],
                        Cantidad = cantidades[i]
                    });
                }
            }
        }

        if (!ModelState.IsValid)
        {
            await CargarOrdenesYRepuestosAsync();
            return View(model);
        }

        var res = await _api.PostAsync<PresupuestoCreateVm, PresupuestoVm>("api/presupuestos", model);
        if (!res.Success || res.Data is null)
        {
            ModelState.AddModelError("", res.ErrorMessage ?? "No se pudo crear el presupuesto.");
            await CargarOrdenesYRepuestosAsync();
            return View(model);
        }

        TempData["Success"] = $"Presupuesto #{res.Data.Id} creado. Total: ${res.Data.Total:F2}.";
        return RedirectToAction(nameof(Details), new { id = res.Data.Id });
    }

    [HttpPost]
    public async Task<IActionResult> Aprobar(int id)
    {
        var res = await _api.PostAsync<object, PresupuestoVm>($"api/presupuestos/{id}/aprobar", new { });
        TempData[res.Success ? "Success" : "Error"] = res.Success
            ? "Presupuesto aprobado e inventario descontado correctamente."
            : res.ErrorMessage;
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task CargarOrdenesYRepuestosAsync()
    {
        var ordenes = await _api.GetAsync<List<OrdenVm>>("api/ordenes");
        var repuestos = await _api.GetAsync<List<RepuestoVm>>("api/inventario");
        ViewBag.Ordenes = (ordenes.Data ?? []).Where(o => !o.TienePresupuesto).ToList();
        ViewBag.Repuestos = repuestos.Data ?? [];
    }
}
