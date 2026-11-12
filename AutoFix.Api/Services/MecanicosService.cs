using AutoFix.Api.Data;
using AutoFix.Api.DTOs;
using AutoFix.Api.Exceptions;
using AutoFix.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoFix.Api.Services;

public class MecanicosService
{
    private readonly AutoFixDbContext _db;

    public MecanicosService(AutoFixDbContext db)
    {
        _db = db;
    }

    public async Task<AsignacionResumenDto> AsignarMecanicoAsync(AsignarMecanicoRequestDto dto)
    {
        if (dto.FechaInicio >= dto.FechaFin)
            throw new BusinessException("La fecha de inicio debe ser menor que la fecha de fin.");

        using var tx = await _db.Database.BeginTransactionAsync();

        var orden = await _db.OrdenesReparacion.FindAsync(dto.OrdenReparacionId)
            ?? throw new NotFoundException($"No existe la orden con Id {dto.OrdenReparacionId}.");

        if (orden.Estado is EstadoOrden.Entregado or EstadoOrden.Cancelado)
            throw new BusinessException("No se puede asignar mecánico a una orden entregada o cancelada.");

        var mecanico = await _db.Mecanicos.FindAsync(dto.MecanicoId)
            ?? throw new NotFoundException($"No existe el mecánico con Id {dto.MecanicoId}.");

        if (!mecanico.Activo)
            throw new BusinessException("El mecánico seleccionado no se encuentra activo.");

        if (dto.MecanicoVersion.HasValue)
            _db.Entry(mecanico).Property(m => m.Version).OriginalValue = dto.MecanicoVersion.Value;

        // Validar cruce de horario: nuevoInicio < finExistente && inicioExistente < nuevoFin
        var hayCruce = await _db.AsignacionesMecanicos.AnyAsync(a =>
            a.MecanicoId == dto.MecanicoId &&
            a.Activa &&
            a.OrdenReparacionId != dto.OrdenReparacionId &&
            dto.FechaInicio < a.FechaFin &&
            a.FechaInicio < dto.FechaFin);

        if (hayCruce)
            throw new ConflictException($"El mecánico '{mecanico.NombreCompleto}' ya tiene una orden asignada que se cruza con ese horario.");

        var asignacionesAnteriores = await _db.AsignacionesMecanicos
            .Where(a => a.OrdenReparacionId == dto.OrdenReparacionId && a.Activa)
            .ToListAsync();
        foreach (var prev in asignacionesAnteriores)
            prev.Activa = false;

        var asignacion = new AsignacionMecanico
        {
            OrdenReparacionId = orden.Id,
            MecanicoId = mecanico.Id,
            FechaInicio = dto.FechaInicio,
            FechaFin = dto.FechaFin,
            Activa = true,
            FechaAsignacion = DateTime.UtcNow
        };

        orden.MecanicoId = mecanico.Id;
        _db.AsignacionesMecanicos.Add(asignacion);

        // Modificar explícitamente la fila del mecánico (su token Version) dentro de la transacción
        _db.Entry(mecanico).Property(m => m.Version).IsModified = true;

        try
        {
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync();
            throw new ConflictException("Otro supervisor asignó al mecánico al mismo tiempo. Intenta de nuevo.");
        }

        return new AsignacionResumenDto
        {
            Id = asignacion.Id,
            OrdenReparacionId = orden.Id,
            NumeroOrden = orden.NumeroOrden,
            MecanicoId = mecanico.Id,
            MecanicoNombre = mecanico.NombreCompleto,
            FechaInicio = asignacion.FechaInicio,
            FechaFin = asignacion.FechaFin,
            Activa = asignacion.Activa
        };
    }
}
