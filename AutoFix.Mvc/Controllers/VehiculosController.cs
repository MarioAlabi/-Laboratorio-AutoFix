using AutoFix.Mvc.Models;
using AutoFix.Mvc.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoFix.Mvc.Controllers;

public class VehiculosController : Controller
{
    private readonly ApiClientService _api;

    public VehiculosController(ApiClientService api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index()
    {
        if (!_api.EstaAutenticado) return RedirectToAction("Login", "Account");

        var res = await _api.GetAsync<List<VehiculoVm>>("api/vehiculos");
        if (!res.Success)
        {
            TempData["Error"] = res.ErrorMessage;
            return View(new List<VehiculoVm>());
        }
        return View(res.Data ?? []);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? clienteId = null)
    {
        if (!_api.EstaAutenticado) return RedirectToAction("Login", "Account");
        await CargarClientesAsync();
        return View(new VehiculoVm { ClienteId = clienteId ?? 0 });
    }

    [HttpPost]
    public async Task<IActionResult> Create(VehiculoVm model, IFormFile? foto)
    {
        if (!ModelState.IsValid)
        {
            await CargarClientesAsync();
            return View(model);
        }

        var res = await _api.PostAsync<VehiculoVm, VehiculoVm>("api/vehiculos", model);
        if (!res.Success || res.Data is null)
        {
            ModelState.AddModelError("", res.ErrorMessage ?? "No se pudo registrar el vehículo.");
            await CargarClientesAsync();
            return View(model);
        }

        if (foto is not null && foto.Length > 0)
        {
            var fotoRes = await _api.PostFileAsync<object>($"api/vehiculos/{res.Data.Id}/foto", foto);
            if (!fotoRes.Success)
            {
                TempData["Error"] = $"Vehículo creado, pero falló la foto: {fotoRes.ErrorMessage}";
                return RedirectToAction(nameof(Index));
            }
        }

        TempData["Success"] = "Vehículo registrado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (!_api.EstaAutenticado) return RedirectToAction("Login", "Account");

        var res = await _api.GetAsync<VehiculoVm>($"api/vehiculos/{id}");
        if (!res.Success || res.Data is null)
        {
            TempData["Error"] = res.ErrorMessage ?? "Vehículo no encontrado.";
            return RedirectToAction(nameof(Index));
        }

        await CargarClientesAsync();
        return View(res.Data);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, VehiculoVm model, IFormFile? foto)
    {
        if (!ModelState.IsValid)
        {
            await CargarClientesAsync();
            return View(model);
        }

        var res = await _api.PutAsync<VehiculoVm, VehiculoVm>($"api/vehiculos/{id}", model);
        if (!res.Success)
        {
            ModelState.AddModelError("", res.ErrorMessage ?? "No se pudo actualizar el vehículo.");
            await CargarClientesAsync();
            return View(model);
        }

        if (foto is not null && foto.Length > 0)
        {
            var fotoRes = await _api.PostFileAsync<object>($"api/vehiculos/{id}/foto", foto);
            if (!fotoRes.Success)
            {
                TempData["Error"] = $"Vehículo actualizado, pero falló la foto: {fotoRes.ErrorMessage}";
                return RedirectToAction(nameof(Index));
            }
        }

        TempData["Success"] = "Vehículo actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var res = await _api.DeleteAsync($"api/vehiculos/{id}");
        TempData[res.Success ? "Success" : "Error"] = res.Success ? "Vehículo eliminado." : res.ErrorMessage;
        return RedirectToAction(nameof(Index));
    }

    private async Task CargarClientesAsync()
    {
        var clientes = await _api.GetAsync<List<ClienteVm>>("api/clientes");
        ViewBag.Clientes = clientes.Data ?? [];
    }
}
