using AutoFix.Mvc.Models;
using AutoFix.Mvc.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoFix.Mvc.Controllers;

public class InventarioController : Controller
{
    private readonly ApiClientService _api;

    public InventarioController(ApiClientService api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(bool soloAlertas = false)
    {
        if (!_api.EstaAutenticado) return RedirectToAction("Login", "Account");

        var res = await _api.GetAsync<List<RepuestoVm>>(soloAlertas ? "api/inventario?soloAlertas=true" : "api/inventario");
        if (!res.Success)
        {
            TempData["Error"] = res.ErrorMessage;
            return View(new List<RepuestoVm>());
        }

        ViewBag.SoloAlertas = soloAlertas;
        return View(res.Data ?? []);
    }

    [HttpGet]
    public IActionResult Create()
    {
        if (!_api.EstaAutenticado) return RedirectToAction("Login", "Account");
        return View(new RepuestoVm());
    }

    [HttpPost]
    public async Task<IActionResult> Create(RepuestoVm model, IFormFile? foto)
    {
        if (!ModelState.IsValid) return View(model);

        var res = await _api.PostAsync<RepuestoVm, RepuestoVm>("api/inventario", model);
        if (!res.Success || res.Data is null)
        {
            ModelState.AddModelError("", res.ErrorMessage ?? "No se pudo registrar el repuesto.");
            return View(model);
        }

        if (foto is not null && foto.Length > 0)
        {
            var fotoRes = await _api.PostFileAsync<object>($"api/inventario/{res.Data.Id}/foto", foto);
            if (!fotoRes.Success)
            {
                TempData["Error"] = $"Repuesto creado, pero falló la foto: {fotoRes.ErrorMessage}";
                return RedirectToAction(nameof(Index));
            }
        }

        TempData["Success"] = "Repuesto registrado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (!_api.EstaAutenticado) return RedirectToAction("Login", "Account");

        var res = await _api.GetAsync<RepuestoVm>($"api/inventario/{id}");
        if (!res.Success || res.Data is null)
        {
            TempData["Error"] = res.ErrorMessage ?? "Repuesto no encontrado.";
            return RedirectToAction(nameof(Index));
        }
        return View(res.Data);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, RepuestoVm model, IFormFile? foto)
    {
        if (!ModelState.IsValid) return View(model);

        var res = await _api.PutAsync<RepuestoVm, RepuestoVm>($"api/inventario/{id}", model);
        if (!res.Success)
        {
            ModelState.AddModelError("", res.ErrorMessage ?? "No se pudo actualizar el repuesto.");
            return View(model);
        }

        if (foto is not null && foto.Length > 0)
        {
            var fotoRes = await _api.PostFileAsync<object>($"api/inventario/{id}/foto", foto);
            if (!fotoRes.Success)
            {
                TempData["Error"] = $"Repuesto actualizado, pero falló la foto: {fotoRes.ErrorMessage}";
                return RedirectToAction(nameof(Index));
            }
        }

        TempData["Success"] = "Repuesto actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }
}
