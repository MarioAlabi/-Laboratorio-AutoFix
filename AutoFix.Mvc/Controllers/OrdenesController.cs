using AutoFix.Mvc.Models;
using AutoFix.Mvc.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoFix.Mvc.Controllers;

public class OrdenesController : Controller
{
    private readonly ApiClientService _api;

    public OrdenesController(ApiClientService api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(int? estado = null)
    {
        if (!_api.EstaAutenticado) return RedirectToAction("Login", "Account");

        var url = estado.HasValue ? $"api/ordenes?estado={estado.Value}" : "api/ordenes";
        var res = await _api.GetAsync<List<OrdenVm>>(url);
        if (!res.Success)
        {
            TempData["Error"] = res.ErrorMessage;
            return View(new List<OrdenVm>());
        }

        ViewBag.EstadoFiltro = estado;
        return View(res.Data ?? []);
    }

    public async Task<IActionResult> Details(int id)
    {
        if (!_api.EstaAutenticado) return RedirectToAction("Login", "Account");

        var res = await _api.GetAsync<OrdenDetalleVm>($"api/ordenes/{id}");
        if (!res.Success || res.Data is null)
        {
            TempData["Error"] = res.ErrorMessage ?? "Orden no encontrada.";
            return RedirectToAction(nameof(Index));
        }
        return View(res.Data);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (!_api.EstaAutenticado) return RedirectToAction("Login", "Account");
        await CargarClientesYVehiculosAsync();
        return View(new OrdenCreateVm());
    }

    [HttpPost]
    public async Task<IActionResult> Create(OrdenCreateVm model)
    {
        if (!ModelState.IsValid)
        {
            await CargarClientesYVehiculosAsync();
            return View(model);
        }

        var res = await _api.PostAsync<OrdenCreateVm, OrdenVm>("api/ordenes", model);
        if (!res.Success || res.Data is null)
        {
            ModelState.AddModelError("", res.ErrorMessage ?? "No se pudo crear la orden.");
            await CargarClientesYVehiculosAsync();
            return View(model);
        }

        TempData["Success"] = $"Orden {res.Data.NumeroOrden} creada en estado Recibido.";
        return RedirectToAction(nameof(Details), new { id = res.Data.Id });
    }

    [HttpPost]
    public async Task<IActionResult> ActualizarDiagnostico(int id, string descripcionProblema, string? diagnostico, DateTime? fechaEstimadaFinalizacion)
    {
        var payload = new { descripcionProblema, diagnostico, fechaEstimadaFinalizacion };
        var res = await _api.PutAsync<object, OrdenVm>($"api/ordenes/{id}", payload);
        TempData[res.Success ? "Success" : "Error"] = res.Success ? "Diagnóstico y orden actualizados." : res.ErrorMessage;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> CambiarEstado(int id, int nuevoEstado)
    {
        var res = await _api.PostAsync<object, object>($"api/ordenes/{id}/estado", new { nuevoEstado });
        TempData[res.Success ? "Success" : "Error"] = res.Success ? "Estado de la orden actualizado." : res.ErrorMessage;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Finalizar(int id)
    {
        var res = await _api.PostAsync<object, object>($"api/ordenes/{id}/finalizar", new { });
        TempData[res.Success ? "Success" : "Error"] = res.Success ? "Orden finalizada." : res.ErrorMessage;
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task CargarClientesYVehiculosAsync()
    {
        var clientes = await _api.GetAsync<List<ClienteVm>>("api/clientes");
        var vehiculos = await _api.GetAsync<List<VehiculoVm>>("api/vehiculos");
        ViewBag.Clientes = clientes.Data ?? [];
        ViewBag.Vehiculos = vehiculos.Data ?? [];
    }
}
