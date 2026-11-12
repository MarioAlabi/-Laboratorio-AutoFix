using System.Security.Claims;
using AutoFix.Api.Data;
using AutoFix.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoFix.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportesController : ControllerBase
{
    private readonly AutoFixDbContext _db;

    public ReportesController(AutoFixDbContext db)
    {
        _db = db;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var ordenesQuery = _db.OrdenesReparacion.AsNoTracking().AsQueryable();
        var esCliente = User.IsInRole(RolesSistema.Cliente);

        if (esCliente)
        {
            var cidStr = User.FindFirstValue("clienteId");
            if (!int.TryParse(cidStr, out var cid))
                return Forbid();
            ordenesQuery = ordenesQuery.Where(o => o.ClienteId == cid);
        }
        else if (User.IsInRole(RolesSistema.Mecanico))
        {
            var midStr = User.FindFirstValue("mecanicoId");
            if (int.TryParse(midStr, out var mid))
                ordenesQuery = ordenesQuery.Where(o => o.MecanicoId == mid);
        }

        var ordenes = await ordenesQuery.ToListAsync();
        var porEstado = ordenes
            .GroupBy(o => o.Estado)
            .Select(g => new
            {
                estado = g.Key.ToString(),
                estadoNombre = OrdenesController.NombreEstado(g.Key),
                cantidad = g.Count()
            })
            .ToList();

        return Ok(new
        {
            totalOrdenes = ordenes.Count,
            ordenesActivas = ordenes.Count(o => o.Estado != EstadoOrden.Entregado && o.Estado != EstadoOrden.Cancelado),
            totalClientes = esCliente ? 1 : await _db.Clientes.CountAsync(),
            totalVehiculos = esCliente
                ? await _db.Vehiculos.CountAsync(v => v.ClienteId == int.Parse(User.FindFirstValue("clienteId")!))
                : await _db.Vehiculos.CountAsync(),
            totalMecanicos = await _db.Mecanicos.CountAsync(m => m.Activo),
            alertasStockMinimo = esCliente ? 0 : await _db.Repuestos.CountAsync(r => r.Existencias <= r.StockMinimo),
            ordenesPorEstado = porEstado
        });
    }

    [HttpGet("operaciones")]
    [Authorize(Policy = "PuedeConsultarReportes")]
    public async Task<IActionResult> Operaciones()
    {
        var ordenes = await _db.OrdenesReparacion
            .Include(o => o.Mecanico)
            .Include(o => o.Presupuesto)
            .AsNoTracking()
            .ToListAsync();

        var presupuestos = await _db.Presupuestos.AsNoTracking().ToListAsync();
        var repuestosAlerta = await _db.Repuestos
            .AsNoTracking()
            .Where(r => r.Existencias <= r.StockMinimo)
            .Select(r => new { r.Id, r.Codigo, r.Nombre, r.Existencias, r.StockMinimo })
            .ToListAsync();

        var cargaMecanicos = await _db.Mecanicos
            .Include(m => m.Asignaciones)
            .AsNoTracking()
            .Select(m => new
            {
                m.Id,
                m.NombreCompleto,
                m.Especialidad,
                asignacionesActivas = m.Asignaciones.Count(a => a.Activa)
            })
            .ToListAsync();

        return Ok(new
        {
            totalOrdenes = ordenes.Count,
            ordenesPorEstado = ordenes.GroupBy(o => o.Estado).Select(g => new
            {
                estado = OrdenesController.NombreEstado(g.Key),
                cantidad = g.Count()
            }),
            totalPresupuestos = presupuestos.Count,
            presupuestosAprobados = presupuestos.Count(p => p.Aprobado),
            montoTotalAprobado = presupuestos.Where(p => p.Aprobado).Sum(p => p.Total),
            repuestosEnAlerta = repuestosAlerta,
            cargaMecanicos
        });
    }

    [HttpGet("logs")]
    [Authorize(Policy = "PuedeConsultarReportes")]
    public async Task<ActionResult<List<LogRegistro>>> Logs(
        [FromQuery] int? codigoHttp = null,
        [FromQuery] string? usuario = null,
        [FromQuery] string? correlationId = null,
        [FromQuery] int limite = 100)
    {
        var query = _db.LogsRegistro.AsNoTracking().AsQueryable();

        if (codigoHttp.HasValue)
            query = query.Where(l => l.CodigoHttp == codigoHttp.Value);

        if (!string.IsNullOrWhiteSpace(usuario))
            query = query.Where(l => l.Usuario.ToLower().Contains(usuario.Trim().ToLower()));

        if (!string.IsNullOrWhiteSpace(correlationId))
            query = query.Where(l => l.CorrelationId.Contains(correlationId.Trim()));

        var logs = await query
            .OrderByDescending(l => l.Id)
            .Take(Math.Clamp(limite, 1, 500))
            .ToListAsync();

        return Ok(logs);
    }
}
