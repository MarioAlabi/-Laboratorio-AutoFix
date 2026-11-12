using AutoFix.Mvc.Models;
using AutoFix.Mvc.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoFix.Mvc.Controllers;

public class HomeController : Controller
{
    private readonly ApiClientService _api;

    public HomeController(ApiClientService api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index()
    {
        if (!_api.EstaAutenticado)
            return RedirectToAction("Login", "Account");

        var res = await _api.GetAsync<DashboardVm>("api/reportes/dashboard");
        if (!res.Success)
        {
            if (res.StatusCode == 401)
            {
                _api.CerrarSesion();
                return RedirectToAction("Login", "Account");
            }
            TempData["Error"] = res.ErrorMessage;
            return View(new DashboardVm());
        }

        return View(res.Data ?? new DashboardVm());
    }
}
