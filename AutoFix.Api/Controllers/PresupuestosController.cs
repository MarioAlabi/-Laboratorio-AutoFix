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
public class PresupuestosController : ControllerBase
{
    private readonly AutoFixDbContext _db;
    private readonly InventarioService _inventario;

    public PresupuestosController(AutoFixDbContext db, InventarioService inventario)
    {
        _db = db;
        _inventario = inventario;
    }

    [HttpGet]
    public async Task<ActionResult<List<PresupuestoDto>>> GetAll()
    {
        var query = _db.Presupuestos
            .Include(p => p.OrdenReparacion)
                .ThenInclude(o => o.Cliente)
            .Include(p => p.OrdenReparacion)
                .ThenInclude(o => o.Vehiculo)
            .Include(p => p.Repuestos)
                .ThenInclude(pr => pr.Repuesto)
            .AsNoTracking()
            .AsQueryable();

        if (User.IsInRole(RolesSistema.Cliente))
        {
            var cidStr = User.FindFirstValue("clienteId");
            if (!int.TryParse(cidStr, out var cid))
                return Forbid();
            query = query.Where(p => p.OrdenReparacion.ClienteId == cid);
        }

        var list = await query.OrderByDescending(p => p.FechaCreacion).Select(p => MapDto(p)).ToListAsync();
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PresupuestoDto>> GetById(int id)
    {
        var p = await _db.Presupuestos
            .Include(x => x.OrdenReparacion)
                .ThenInclude(o => o.Cliente)
            .Include(x => x.OrdenReparacion)
                .ThenInclude(o => o.Vehiculo)
            .Include(x => x.Repuestos)
                .ThenInclude(pr => pr.Repuesto)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException($"No existe el presupuesto con Id {id}.");

        if (User.IsInRole(RolesSistema.Cliente))
        {
            var cidStr = User.FindFirstValue("clienteId");
            if (!int.TryParse(cidStr, out var cid) || p.OrdenReparacion.ClienteId != cid)
                return Forbid();
        }

        return Ok(MapDto(p));
    }

    [HttpPost]
    [Authorize(Policy = "PuedeEditarOrden")]
    public async Task<ActionResult<PresupuestoDto>> Create(PresupuestoCreateDto dto)
    {
        var orden = await _db.OrdenesReparacion
            .Include(o => o.Cliente)
            .Include(o => o.Vehiculo)
            .Include(o => o.Presupuesto)
            .FirstOrDefaultAsync(o => o.Id == dto.OrdenReparacionId)
            ?? throw new NotFoundException($"No existe la orden con Id {dto.OrdenReparacionId}.");

        if (orden.Presupuesto is not null)
            throw new ConflictException("La orden ya cuenta con un presupuesto registrado.");

        if (orden.Estado is not (EstadoOrden.Recibido or EstadoOrden.EnDiagnostico or EstadoOrden.Presupuestado))
            throw new BusinessException("No se puede crear presupuesto para una orden en el estado actual.");

        var presupuesto = new Presupuesto
        {
            OrdenReparacionId = orden.Id,
            ManoDeObra = dto.ManoDeObra,
            Observaciones = dto.Observaciones?.Trim(),
            Aprobado = false
        };

        decimal subtotalRepuestos = 0;
        foreach (var item in dto.Repuestos)
        {
            var repuesto = await _db.Repuestos.FindAsync(item.RepuestoId)
                ?? throw new NotFoundException($"No existe el repuesto con Id {item.RepuestoId}.");

            if (repuesto.Existencias < item.Cantidad)
                throw new BusinessException($"Existencias insuficientes para '{repuesto.Nombre}'. Disponibles: {repuesto.Existencias}.");

            var subtotal = item.Cantidad * repuesto.Precio;
            subtotalRepuestos += subtotal;

            presupuesto.Repuestos.Add(new PresupuestoRepuesto
            {
                RepuestoId = repuesto.Id,
                Cantidad = item.Cantidad,
                PrecioUnitario = repuesto.Precio,
                Subtotal = subtotal
            });
        }

        presupuesto.Total = dto.ManoDeObra + subtotalRepuestos;
        orden.Estado = dto.EnviarAAprobacion ? EstadoOrden.PendienteAprobacion : EstadoOrden.Presupuestado;

        _db.Presupuestos.Add(presupuesto);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = presupuesto.Id }, await GetPresupuestoDtoAsync(presupuesto.Id));
    }

    [Authorize(Policy = "PuedeAprobar")]
    [HttpPost("{id:int}/aprobar")]
    public async Task<ActionResult<PresupuestoDto>> Aprobar(int id)
    {
        using var tx = await _db.Database.BeginTransactionAsync();

        var presupuesto = await _db.Presupuestos
            .Include(p => p.OrdenReparacion)
            .Include(p => p.Repuestos)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException($"No existe el presupuesto con Id {id}.");

        if (presupuesto.Aprobado)
            throw new ConflictException("El presupuesto ya fue aprobado anteriormente.");

        var usuario = User.Identity?.Name ?? "Supervisor";

        await _inventario.DescontarParaPresupuestoAsync(presupuesto, usuario);

        presupuesto.Aprobado = true;
        presupuesto.FechaAprobacion = DateTime.UtcNow;
        presupuesto.AprobadoPor = usuario;
        presupuesto.OrdenReparacion.Estado = EstadoOrden.Aprobado;

        try
        {
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync();
            throw new ConflictException("Conflicto de concurrencia al descontar repuestos durante la aprobación. Intente nuevamente.");
        }

        return Ok(await GetPresupuestoDtoAsync(presupuesto.Id));
    }

    private async Task<PresupuestoDto> GetPresupuestoDtoAsync(int id)
    {
        var p = await _db.Presupuestos
            .Include(x => x.OrdenReparacion).ThenInclude(o => o.Cliente)
            .Include(x => x.OrdenReparacion).ThenInclude(o => o.Vehiculo)
            .Include(x => x.Repuestos).ThenInclude(pr => pr.Repuesto)
            .AsNoTracking()
            .FirstAsync(x => x.Id == id);
        return MapDto(p);
    }

    private static PresupuestoDto MapDto(Presupuesto p) => new()
    {
        Id = p.Id,
        OrdenReparacionId = p.OrdenReparacionId,
        NumeroOrden = p.OrdenReparacion.NumeroOrden,
        VehiculoPlaca = p.OrdenReparacion.Vehiculo.Placa,
        ClienteNombre = p.OrdenReparacion.Cliente.NombreCompleto,
        ManoDeObra = p.ManoDeObra,
        Total = p.Total,
        Observaciones = p.Observaciones,
        Aprobado = p.Aprobado,
        FechaCreacion = p.FechaCreacion,
        FechaAprobacion = p.FechaAprobacion,
        AprobadoPor = p.AprobadoPor,
        Repuestos = p.Repuestos.Select(pr => new PresupuestoRepuestoDto
        {
            Id = pr.Id,
            RepuestoId = pr.RepuestoId,
            RepuestoCodigo = pr.Repuesto.Codigo,
            RepuestoNombre = pr.Repuesto.Nombre,
            Cantidad = pr.Cantidad,
            PrecioUnitario = pr.PrecioUnitario,
            Subtotal = pr.Subtotal
        }).ToList()
    };
}
