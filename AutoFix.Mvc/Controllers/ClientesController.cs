using AutoFix.Mvc.Models;
using AutoFix.Mvc.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoFix.Mvc.Controllers;

public class ClientesController : Controller
{
    private readonly ApiClientService _api;

    public ClientesController(ApiClientService api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index()
    {
        if (!_api.EstaAutenticado) return RedirectToAction("Login", "Account");

        var res = await _api.GetAsync<List<ClienteVm>>("api/clientes");
        if (!res.Success)
        {
            TempData["Error"] = res.ErrorMessage;
            return View(new List<ClienteVm>());
        }
        return View(res.Data ?? []);
    }

    public async Task<IActionResult> Details(int id)
    {
        if (!_api.EstaAutenticado) return RedirectToAction("Login", "Account");

        var res = await _api.GetAsync<ClienteVm>($"api/clientes/{id}");
        if (!res.Success || res.Data is null)
        {
            TempData["Error"] = res.ErrorMessage ?? "Cliente no encontrado.";
            return RedirectToAction(nameof(Index));
        }
        return View(res.Data);
    }

    [HttpGet]
    public IActionResult Create()
    {
        if (!_api.EstaAutenticado) return RedirectToAction("Login", "Account");
        return View(new ClienteVm());
    }

    [HttpPost]
    public async Task<IActionResult> Create(ClienteVm model)
    {
        if (!ModelState.IsValid) return View(model);

        var res = await _api.PostAsync<ClienteVm, ClienteVm>("api/clientes", model);
        if (!res.Success)
        {
            ModelState.AddModelError("", res.ErrorMessage ?? "No se pudo crear el cliente.");
            return View(model);
        }

        TempData["Success"] = "Cliente registrado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (!_api.EstaAutenticado) return RedirectToAction("Login", "Account");

        var res = await _api.GetAsync<ClienteVm>($"api/clientes/{id}");
        if (!res.Success || res.Data is null)
        {
            TempData["Error"] = res.ErrorMessage ?? "Cliente no encontrado.";
            return RedirectToAction(nameof(Index));
        }
        return View(res.Data);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, ClienteVm model)
    {
        if (!ModelState.IsValid) return View(model);

        var res = await _api.PutAsync<ClienteVm, ClienteVm>($"api/clientes/{id}", model);
        if (!res.Success)
        {
            ModelState.AddModelError("", res.ErrorMessage ?? "No se pudo actualizar el cliente.");
            return View(model);
        }

        TempData["Success"] = "Cliente actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var res = await _api.DeleteAsync($"api/clientes/{id}");
        TempData[res.Success ? "Success" : "Error"] = res.Success ? "Cliente eliminado." : res.ErrorMessage;
        return RedirectToAction(nameof(Index));
    }
}
