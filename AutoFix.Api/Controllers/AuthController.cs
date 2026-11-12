using System.Security.Claims;
using AutoFix.Api.Data;
using AutoFix.Api.DTOs;
using AutoFix.Api.Models;
using AutoFix.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoFix.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AutoFixDbContext _db;
    private readonly JwtService _jwt;
    private readonly PasswordHasher<Usuario> _hasher = new();

    public AuthController(AutoFixDbContext db, JwtService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Usuarios.AnyAsync(u => u.Email.ToLower() == email))
            return Conflict(new { mensaje = "Ya existe un usuario registrado con ese correo electrónico." });

        var rol = string.IsNullOrWhiteSpace(request.Rol) ? RolesSistema.Cliente : request.Rol.Trim();
        if (!RolesSistema.Todos.Contains(rol))
            return BadRequest(new { mensaje = $"Rol inválido. Roles permitidos: {string.Join(", ", RolesSistema.Todos)}" });

        var usuario = new Usuario
        {
            NombreCompleto = request.NombreCompleto.Trim(),
            Email = email,
            Rol = rol
        };
        usuario.PasswordHash = _hasher.HashPassword(usuario, request.Password);

        var claimsPorDefecto = ObtenerClaimsPorRol(rol);
        foreach (var c in claimsPorDefecto)
        {
            usuario.Claims.Add(new UsuarioClaim
            {
                ClaimType = ClaimsSistema.TipoPermiso,
                ClaimValue = c
            });
        }

        if (rol == RolesSistema.Cliente)
        {
            var cliente = new Cliente
            {
                NombreCompleto = usuario.NombreCompleto,
                Dui = $"REG-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                Telefono = "0000-0000",
                CorreoElectronico = email,
                Direccion = "Por definir"
            };
            _db.Clientes.Add(cliente);
            await _db.SaveChangesAsync();
            usuario.ClienteId = cliente.Id;
        }

        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();

        var (token, exp) = _jwt.GenerarToken(usuario);
        return Ok(ConstruirRespuesta(usuario, token, exp));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var usuario = await _db.Usuarios
            .Include(u => u.Claims)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

        if (usuario is null)
            return Unauthorized(new { mensaje = "Credenciales inválidas." });

        var result = _hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
            return Unauthorized(new { mensaje = "Credenciales inválidas." });

        var (token, exp) = _jwt.GenerarToken(usuario);
        return Ok(ConstruirRespuesta(usuario, token, exp));
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        return Ok(new
        {
            usuario = User.Identity?.Name,
            rol = User.FindFirstValue(ClaimTypes.Role),
            permisos = User.FindAll(ClaimsSistema.TipoPermiso).Select(c => c.Value).ToList()
        });
    }

    private static string[] ObtenerClaimsPorRol(string rol) => rol switch
    {
        RolesSistema.Administrador => ClaimsSistema.Todos,
        RolesSistema.Recepcionista => [ClaimsSistema.OrdenesCrear, ClaimsSistema.OrdenesEditar],
        RolesSistema.Mecanico => [ClaimsSistema.OrdenesEditar],
        RolesSistema.Supervisor =>
        [
            ClaimsSistema.OrdenesAsignar,
            ClaimsSistema.OrdenesAprobar,
            ClaimsSistema.OrdenesFinalizar,
            ClaimsSistema.InventarioGestionar,
            ClaimsSistema.ReportesConsultar
        ],
        _ => []
    };

    private static AuthResponse ConstruirRespuesta(Usuario u, string token, DateTime exp) => new()
    {
        Token = token,
        Expiracion = exp,
        UsuarioId = u.Id,
        NombreCompleto = u.NombreCompleto,
        Email = u.Email,
        Rol = u.Rol,
        Claims = u.Claims.Select(c => c.ClaimValue).ToList(),
        ClienteId = u.ClienteId,
        MecanicoId = u.MecanicoId
    };
}
