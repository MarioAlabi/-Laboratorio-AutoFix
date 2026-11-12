using System.Security.Claims;
using AutoFix.Api.Data;
using AutoFix.Api.DTOs;
using AutoFix.Api.Exceptions;
using AutoFix.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoFix.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdenesController : ControllerBase
{
    private readonly AutoFixDbContext _db;

    public OrdenesController(AutoFixDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<OrdenDto>>> GetAll([FromQuery] EstadoOrden? estado = null)
    {
        var query = _db.OrdenesReparacion
            .Include(o => o.Cliente)
            .Include(o => o.Vehiculo)
            .Include(o => o.Mecanico)
            .Include(o => o.Presupuesto)
            .Include(o => o.Evidencias)
            .AsNoTracking()
            .AsQueryable();

        if (User.IsInRole(RolesSistema.Cliente))
        {
            var cidStr = User.FindFirstValue("clienteId");
            if (!int.TryParse(cidStr, out var cid))
                return Forbid();
            query = query.Where(o => o.ClienteId == cid);
        }
        else if (User.IsInRole(RolesSistema.Mecanico))
        {
            var midStr = User.FindFirstValue("mecanicoId");
            if (int.TryParse(midStr, out var mid))
                query = query.Where(o => o.MecanicoId == mid);
        }

        if (estado.HasValue)
            query = query.Where(o => o.Estado == estado.Value);

        var list = await query
            .OrderByDescending(o => o.FechaIngreso)
            .Select(o => new OrdenDto
            {
                Id = o.Id,
                NumeroOrden = o.NumeroOrden,
                ClienteId = o.ClienteId,
                ClienteNombre = o.Cliente.NombreCompleto,
                VehiculoId = o.VehiculoId,
                VehiculoPlaca = o.Vehiculo.Placa,
                VehiculoDescripcion = $"{o.Vehiculo.Marca} {o.Vehiculo.Modelo} ({o.Vehiculo.Anio})",
                FechaIngreso = o.FechaIngreso,
                FechaEstimadaFinalizacion = o.FechaEstimadaFinalizacion,
                DescripcionProblema = o.DescripcionProblema,
                Diagnostico = o.Diagnostico,
                MecanicoId = o.MecanicoId,
                MecanicoNombre = o.Mecanico != null ? o.Mecanico.NombreCompleto : null,
                Estado = o.Estado,
                EstadoNombre = NombreEstado(o.Estado),
                TienePresupuesto = o.Presupuesto != null,
                PresupuestoAprobado = o.Presupuesto != null && o.Presupuesto.Aprobado,
                CantidadEvidencias = o.Evidencias.Count
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrdenDetalleDto>> GetById(int id)
    {
        var o = await _db.OrdenesReparacion
            .Include(x => x.Cliente)
            .Include(x => x.Vehiculo)
            .Include(x => x.Mecanico)
            .Include(x => x.Presupuesto!)
                .ThenInclude(p => p.Repuestos)
                    .ThenInclude(pr => pr.Repuesto)
            .Include(x => x.Evidencias)
            .Include(x => x.Asignaciones)
                .ThenInclude(a => a.Mecanico)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException($"No existe la orden con Id {id}.");

        if (User.IsInRole(RolesSistema.Cliente))
        {
            var cidStr = User.FindFirstValue("clienteId");
            if (!int.TryParse(cidStr, out var cid) || o.ClienteId != cid)
                return Forbid();
        }

        return Ok(new OrdenDetalleDto
        {
            Id = o.Id,
            NumeroOrden = o.NumeroOrden,
            ClienteId = o.ClienteId,
            ClienteNombre = o.Cliente.NombreCompleto,
            VehiculoId = o.VehiculoId,
            VehiculoPlaca = o.Vehiculo.Placa,
            VehiculoDescripcion = $"{o.Vehiculo.Marca} {o.Vehiculo.Modelo} ({o.Vehiculo.Anio})",
            FechaIngreso = o.FechaIngreso,
            FechaEstimadaFinalizacion = o.FechaEstimadaFinalizacion,
            DescripcionProblema = o.DescripcionProblema,
            Diagnostico = o.Diagnostico,
            MecanicoId = o.MecanicoId,
            MecanicoNombre = o.Mecanico?.NombreCompleto,
            Estado = o.Estado,
            EstadoNombre = NombreEstado(o.Estado),
            TienePresupuesto = o.Presupuesto != null,
            PresupuestoAprobado = o.Presupuesto?.Aprobado ?? false,
            CantidadEvidencias = o.Evidencias.Count,
            Presupuesto = o.Presupuesto is null ? null : new PresupuestoDto
            {
                Id = o.Presupuesto.Id,
                OrdenReparacionId = o.Id,
                NumeroOrden = o.NumeroOrden,
                VehiculoPlaca = o.Vehiculo.Placa,
                ClienteNombre = o.Cliente.NombreCompleto,
                ManoDeObra = o.Presupuesto.ManoDeObra,
                Total = o.Presupuesto.Total,
                Observaciones = o.Presupuesto.Observaciones,
                Aprobado = o.Presupuesto.Aprobado,
                FechaCreacion = o.Presupuesto.FechaCreacion,
                FechaAprobacion = o.Presupuesto.FechaAprobacion,
                AprobadoPor = o.Presupuesto.AprobadoPor,
                Repuestos = o.Presupuesto.Repuestos.Select(pr => new PresupuestoRepuestoDto
                {
                    Id = pr.Id,
                    RepuestoId = pr.RepuestoId,
                    RepuestoCodigo = pr.Repuesto.Codigo,
                    RepuestoNombre = pr.Repuesto.Nombre,
                    Cantidad = pr.Cantidad,
                    PrecioUnitario = pr.PrecioUnitario,
                    Subtotal = pr.Subtotal
                }).ToList()
            },
            Evidencias = o.Evidencias.OrderByDescending(ev => ev.FechaRegistro).Select(ev => new EvidenciaDto
            {
                Id = ev.Id,
                OrdenReparacionId = ev.OrdenReparacionId,
                Categoria = ev.Categoria,
                Ruta = ev.Ruta,
                Descripcion = ev.Descripcion,
                FechaRegistro = ev.FechaRegistro,
                SubidoPor = ev.SubidoPor
            }).ToList(),
            Asignaciones = o.Asignaciones.OrderByDescending(a => a.FechaAsignacion).Select(a => new AsignacionResumenDto
            {
                Id = a.Id,
                OrdenReparacionId = a.OrdenReparacionId,
                NumeroOrden = o.NumeroOrden,
                MecanicoId = a.MecanicoId,
                MecanicoNombre = a.Mecanico.NombreCompleto,
                FechaInicio = a.FechaInicio,
                FechaFin = a.FechaFin,
                Activa = a.Activa
            }).ToList()
        });
    }

    [HttpPost]
    [Authorize(Policy = "PuedeCrearOrden")]
    public async Task<ActionResult<OrdenDto>> Create(OrdenCreateDto dto)
    {
        var vehiculo = await _db.Vehiculos
            .Include(v => v.Cliente)
            .FirstOrDefaultAsync(v => v.Id == dto.VehiculoId)
            ?? throw new NotFoundException($"No existe el vehículo con Id {dto.VehiculoId}.");

        if (vehiculo.ClienteId != dto.ClienteId)
            throw new BusinessException("El vehículo seleccionado no pertenece al cliente indicado.");

        var descripcion = dto.DescripcionProblema.Trim();
        var duplicada = await _db.OrdenesReparacion.AnyAsync(o =>
            o.VehiculoId == dto.VehiculoId &&
            o.Estado != EstadoOrden.Entregado &&
            o.Estado != EstadoOrden.Cancelado &&
            o.DescripcionProblema.ToLower() == descripcion.ToLower());

        if (duplicada)
            throw new ConflictException("Ya existe una orden activa igual para este vehículo y servicio.");

        var count = await _db.OrdenesReparacion.CountAsync();
        var numeroOrden = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{(count + 1):D4}";

        var orden = new OrdenReparacion
        {
            NumeroOrden = numeroOrden,
            ClienteId = vehiculo.ClienteId,
            VehiculoId = vehiculo.Id,
            FechaIngreso = DateTime.UtcNow,
            FechaEstimadaFinalizacion = dto.FechaEstimadaFinalizacion,
            DescripcionProblema = descripcion,
            Estado = EstadoOrden.Recibido
        };

        _db.OrdenesReparacion.Add(orden);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = orden.Id }, new OrdenDto
        {
            Id = orden.Id,
            NumeroOrden = orden.NumeroOrden,
            ClienteId = orden.ClienteId,
            ClienteNombre = vehiculo.Cliente.NombreCompleto,
            VehiculoId = orden.VehiculoId,
            VehiculoPlaca = vehiculo.Placa,
            VehiculoDescripcion = $"{vehiculo.Marca} {vehiculo.Modelo} ({vehiculo.Anio})",
            FechaIngreso = orden.FechaIngreso,
            FechaEstimadaFinalizacion = orden.FechaEstimadaFinalizacion,
            DescripcionProblema = orden.DescripcionProblema,
            Estado = orden.Estado,
            EstadoNombre = NombreEstado(orden.Estado)
        });
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "PuedeEditarOrden")]
    public async Task<ActionResult<OrdenDto>> Update(int id, OrdenUpdateDto dto)
    {
        var orden = await _db.OrdenesReparacion
            .Include(o => o.Cliente)
            .Include(o => o.Vehiculo)
            .Include(o => o.Mecanico)
            .FirstOrDefaultAsync(o => o.Id == id)
            ?? throw new NotFoundException($"No existe la orden con Id {id}.");

        if (User.IsInRole(RolesSistema.Mecanico))
        {
            var midStr = User.FindFirstValue("mecanicoId");
            if (int.TryParse(midStr, out var mid) && orden.MecanicoId.HasValue && orden.MecanicoId.Value != mid)
                throw new ForbiddenException("Un mecánico solo puede modificar las órdenes que tiene asignadas.");
        }

        orden.DescripcionProblema = dto.DescripcionProblema.Trim();
        orden.Diagnostico = dto.Diagnostico?.Trim();
        orden.FechaEstimadaFinalizacion = dto.FechaEstimadaFinalizacion;

        await _db.SaveChangesAsync();

        return Ok(new OrdenDto
        {
            Id = orden.Id,
            NumeroOrden = orden.NumeroOrden,
            ClienteId = orden.ClienteId,
            ClienteNombre = orden.Cliente.NombreCompleto,
            VehiculoId = orden.VehiculoId,
            VehiculoPlaca = orden.Vehiculo.Placa,
            VehiculoDescripcion = $"{orden.Vehiculo.Marca} {orden.Vehiculo.Modelo} ({orden.Vehiculo.Anio})",
            FechaIngreso = orden.FechaIngreso,
            FechaEstimadaFinalizacion = orden.FechaEstimadaFinalizacion,
            DescripcionProblema = orden.DescripcionProblema,
            Diagnostico = orden.Diagnostico,
            MecanicoId = orden.MecanicoId,
            MecanicoNombre = orden.Mecanico?.NombreCompleto,
            Estado = orden.Estado,
            EstadoNombre = NombreEstado(orden.Estado)
        });
    }

    [HttpPost("{id:int}/estado")]
    public async Task<IActionResult> CambiarEstado(int id, CambiarEstadoDto dto)
    {
        var orden = await _db.OrdenesReparacion.FindAsync(id)
            ?? throw new NotFoundException($"No existe la orden con Id {id}.");

        var actual = orden.Estado;
        var siguiente = dto.NuevoEstado;

        if (actual == EstadoOrden.Entregado || actual == EstadoOrden.Cancelado)
            throw new BusinessException("La orden ya se encuentra en un estado terminal y no puede cambiar.");

        if (siguiente == EstadoOrden.Cancelado)
        {
            if (!User.HasClaim(ClaimsSistema.TipoPermiso, ClaimsSistema.OrdenesFinalizar) &&
                !User.IsInRole(RolesSistema.Administrador))
                throw new ForbiddenException("No tiene permiso para cancelar órdenes.");
        }
        else
        {
            var valido = (actual, siguiente) switch
            {
                (EstadoOrden.Recibido, EstadoOrden.EnDiagnostico) => ClaimsSistema.OrdenesEditar,
                (EstadoOrden.EnDiagnostico, EstadoOrden.Presupuestado) => ClaimsSistema.OrdenesEditar,
                (EstadoOrden.Presupuestado, EstadoOrden.PendienteAprobacion) => ClaimsSistema.OrdenesEditar,
                (EstadoOrden.PendienteAprobacion, EstadoOrden.Aprobado) => ClaimsSistema.OrdenesAprobar,
                (EstadoOrden.Aprobado, EstadoOrden.EnReparacion) => ClaimsSistema.OrdenesEditar,
                (EstadoOrden.EnReparacion, EstadoOrden.Finalizado) => ClaimsSistema.OrdenesFinalizar,
                (EstadoOrden.Finalizado, EstadoOrden.Entregado) => ClaimsSistema.OrdenesEditar,
                _ => null
            };

            if (valido is null)
                throw new BusinessException($"Transición inválida de '{NombreEstado(actual)}' a '{NombreEstado(siguiente)}'. No se pueden saltar pasos.");

            if (!User.HasClaim(ClaimsSistema.TipoPermiso, valido))
                throw new ForbiddenException($"No cuenta con el permiso '{valido}' requerido para esta transición.");
        }

        orden.Estado = siguiente;
        await _db.SaveChangesAsync();

        return Ok(new { id = orden.Id, numeroOrden = orden.NumeroOrden, estado = orden.Estado, estadoNombre = NombreEstado(orden.Estado) });
    }

    [HttpPost("{id:int}/finalizar")]
    [Authorize(Policy = "PuedeFinalizar")]
    public async Task<IActionResult> Finalizar(int id)
    {
        var orden = await _db.OrdenesReparacion.FindAsync(id)
            ?? throw new NotFoundException($"No existe la orden con Id {id}.");

        if (orden.Estado != EstadoOrden.EnReparacion)
            throw new BusinessException($"Solo se puede finalizar una orden en estado 'En reparación'. Estado actual: '{NombreEstado(orden.Estado)}'.");

        orden.Estado = EstadoOrden.Finalizado;
        foreach (var asig in await _db.AsignacionesMecanicos.Where(a => a.OrdenReparacionId == id && a.Activa).ToListAsync())
            asig.Activa = false;

        await _db.SaveChangesAsync();
        return Ok(new { id = orden.Id, numeroOrden = orden.NumeroOrden, estado = orden.Estado, estadoNombre = NombreEstado(orden.Estado) });
    }

    public static string NombreEstado(EstadoOrden estado) => estado switch
    {
        EstadoOrden.Recibido => "Recibido",
        EstadoOrden.EnDiagnostico => "En diagnóstico",
        EstadoOrden.Presupuestado => "Presupuestado",
        EstadoOrden.PendienteAprobacion => "Pendiente de aprobación",
        EstadoOrden.Aprobado => "Aprobado",
        EstadoOrden.EnReparacion => "En reparación",
        EstadoOrden.Finalizado => "Finalizado",
        EstadoOrden.Entregado => "Entregado",
        EstadoOrden.Cancelado => "Cancelado",
        _ => estado.ToString()
    };
}
