using AutoFix.Mvc.Models;
using AutoFix.Mvc.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoFix.Mvc.Controllers;

public class AccountController : Controller
{
    private readonly ApiClientService _api;

    public AccountController(ApiClientService api)
    {
        _api = api;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (_api.EstaAutenticado)
            return RedirectToAction("Index", "Home");
        return View(new LoginVm());
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginVm model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var res = await _api.PostAsync<LoginVm, AuthResponseVm>("api/auth/login", model);
        if (!res.Success || res.Data is null)
        {
            ModelState.AddModelError("", res.ErrorMessage ?? "Credenciales inválidas.");
            return View(model);
        }

        _api.GuardarSesion(res.Data);
        TempData["Success"] = $"Bienvenido, {res.Data.NombreCompleto} ({res.Data.Rol}).";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterVm());

    [HttpPost]
    public async Task<IActionResult> Register(RegisterVm model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var res = await _api.PostAsync<RegisterVm, AuthResponseVm>("api/auth/register", model);
        if (!res.Success || res.Data is null)
        {
            ModelState.AddModelError("", res.ErrorMessage ?? "No se pudo completar el registro.");
            return View(model);
        }

        _api.GuardarSesion(res.Data);
        TempData["Success"] = "Cuenta creada e inicio de sesión exitoso.";
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public IActionResult Logout()
    {
        _api.CerrarSesion();
        return RedirectToAction(nameof(Login));
    }
}
