using System.Security.Claims;
using AutoFix.Api.Data;
using AutoFix.Api.DTOs;
using AutoFix.Api.Exceptions;
using AutoFix.Api.Models;
using AutoFix.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoFix.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EvidenciasController : ControllerBase
{
    private readonly AutoFixDbContext _db;
    private readonly ImagenStorageService _imagenes;

    public EvidenciasController(AutoFixDbContext db, ImagenStorageService imagenes)
    {
        _db = db;
        _imagenes = imagenes;
    }

    [HttpGet]
    public async Task<ActionResult<List<EvidenciaDto>>> GetAll([FromQuery] int? ordenId = null)
    {
        var query = _db.Evidencias
            .Include(e => e.OrdenReparacion)
            .AsNoTracking()
            .AsQueryable();

        if (User.IsInRole(RolesSistema.Cliente))
        {
            var cidStr = User.FindFirstValue("clienteId");
            if (!int.TryParse(cidStr, out var cid))
                return Forbid();
            query = query.Where(e => e.OrdenReparacion.ClienteId == cid);
        }

        if (ordenId.HasValue)
            query = query.Where(e => e.OrdenReparacionId == ordenId.Value);

        var list = await query
            .OrderByDescending(e => e.FechaRegistro)
            .Select(e => new EvidenciaDto
            {
                Id = e.Id,
                OrdenReparacionId = e.OrdenReparacionId,
                Categoria = e.Categoria,
                Ruta = e.Ruta,
                Descripcion = e.Descripcion,
                FechaRegistro = e.FechaRegistro,
                SubidoPor = e.SubidoPor
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpGet("/api/ordenes/{ordenId:int}/evidencias")]
    public async Task<ActionResult<List<EvidenciaDto>>> GetPorOrden(int ordenId)
    {
        var orden = await _db.OrdenesReparacion.AsNoTracking().FirstOrDefaultAsync(o => o.Id == ordenId)
            ?? throw new NotFoundException($"No existe la orden con Id {ordenId}.");

        if (User.IsInRole(RolesSistema.Cliente))
        {
            var cidStr = User.FindFirstValue("clienteId");
            if (!int.TryParse(cidStr, out var cid) || orden.ClienteId != cid)
                return Forbid();
        }

        var list = await _db.Evidencias
            .Where(e => e.OrdenReparacionId == ordenId)
            .OrderByDescending(e => e.FechaRegistro)
            .Select(e => new EvidenciaDto
            {
                Id = e.Id,
                OrdenReparacionId = e.OrdenReparacionId,
                Categoria = e.Categoria,
                Ruta = e.Ruta,
                Descripcion = e.Descripcion,
                FechaRegistro = e.FechaRegistro,
                SubidoPor = e.SubidoPor
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpPost("/api/ordenes/{ordenId:int}/evidencias")]
    [Authorize(Roles = $"{RolesSistema.Administrador},{RolesSistema.Recepcionista},{RolesSistema.Mecanico},{RolesSistema.Supervisor}")]
    public async Task<ActionResult<EvidenciaDto>> Subir(
        int ordenId,
        IFormFile archivo,
        [FromForm] string? categoria = null,
        [FromForm] string? descripcion = null)
    {
        var orden = await _db.OrdenesReparacion.FindAsync(ordenId)
            ?? throw new NotFoundException($"No existe la orden con Id {ordenId}.");

        var cat = string.IsNullOrWhiteSpace(categoria)
            ? CategoriasEvidencia.DanioIdentificado
            : categoria.Trim();

        if (!CategoriasEvidencia.Todas.Contains(cat))
            throw new BusinessException($"Categoría inválida. Permitidas: {string.Join(", ", CategoriasEvidencia.Todas)}");

        var ruta = await _imagenes.GuardarImagenAsync(archivo);

        var evidencia = new Evidencia
        {
            OrdenReparacionId = orden.Id,
            Categoria = cat,
            Ruta = ruta,
            Descripcion = descripcion?.Trim(),
            FechaRegistro = DateTime.UtcNow,
            SubidoPor = User.Identity?.Name
        };

        _db.Evidencias.Add(evidencia);
        await _db.SaveChangesAsync();

        return Ok(new EvidenciaDto
        {
            Id = evidencia.Id,
            OrdenReparacionId = evidencia.OrdenReparacionId,
            Categoria = evidencia.Categoria,
            Ruta = evidencia.Ruta,
            Descripcion = evidencia.Descripcion,
            FechaRegistro = evidencia.FechaRegistro,
            SubidoPor = evidencia.SubidoPor
        });
    }
}
