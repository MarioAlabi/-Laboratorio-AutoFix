using AutoFix.Mvc.Models;
using AutoFix.Mvc.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoFix.Mvc.Controllers;

public class MecanicosController : Controller
{
    private readonly ApiClientService _api;

    public MecanicosController(ApiClientService api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index()
    {
        if (!_api.EstaAutenticado) return RedirectToAction("Login", "Account");

        var res = await _api.GetAsync<List<MecanicoVm>>("api/mecanicos");
        if (!res.Success)
        {
            TempData["Error"] = res.ErrorMessage;
            return View(new List<MecanicoVm>());
        }
        return View(res.Data ?? []);
    }

    [HttpGet]
    public IActionResult Create()
    {
        if (!_api.EstaAutenticado) return RedirectToAction("Login", "Account");
        return View(new MecanicoVm());
    }

    [HttpPost]
    public async Task<IActionResult> Create(MecanicoVm model)
    {
        if (!ModelState.IsValid) return View(model);

        var res = await _api.PostAsync<MecanicoVm, MecanicoVm>("api/mecanicos", model);
        if (!res.Success)
        {
            ModelState.AddModelError("", res.ErrorMessage ?? "No se pudo registrar el mecánico.");
            return View(model);
        }

        TempData["Success"] = "Mecánico registrado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Asignar(int? ordenId = null, int? mecanicoId = null)
    {
        if (!_api.EstaAutenticado) return RedirectToAction("Login", "Account");
        await CargarOrdenesYMecanicosAsync();
        return View(new AsignarMecanicoVm
        {
            OrdenReparacionId = ordenId ?? 0,
            MecanicoId = mecanicoId ?? 0,
            FechaInicio = DateTime.Now,
            FechaFin = DateTime.Now.AddHours(2)
        });
    }

    [HttpPost]
    public async Task<IActionResult> Asignar(AsignarMecanicoVm model)
    {
        if (!ModelState.IsValid)
        {
            await CargarOrdenesYMecanicosAsync();
            return View(model);
        }

        var res = await _api.PostAsync<AsignarMecanicoVm, AsignacionVm>("api/mecanicos/asignar", model);
        if (!res.Success)
        {
            ModelState.AddModelError("", res.ErrorMessage ?? "No se pudo asignar al mecánico.");
            await CargarOrdenesYMecanicosAsync();
            return View(model);
        }

        TempData["Success"] = $"Mecánico '{res.Data?.MecanicoNombre}' asignado a la orden {res.Data?.NumeroOrden}.";
        return RedirectToAction("Details", "Ordenes", new { id = model.OrdenReparacionId });
    }

    private async Task CargarOrdenesYMecanicosAsync()
    {
        var ordenes = await _api.GetAsync<List<OrdenVm>>("api/ordenes");
        var mecanicos = await _api.GetAsync<List<MecanicoVm>>("api/mecanicos");
        ViewBag.Ordenes = (ordenes.Data ?? []).Where(o => o.Estado != 8 && o.Estado != 9).ToList();
        ViewBag.Mecanicos = mecanicos.Data ?? [];
    }
}
