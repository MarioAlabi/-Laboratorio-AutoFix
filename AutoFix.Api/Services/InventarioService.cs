using AutoFix.Api.Data;
using AutoFix.Api.Exceptions;
using AutoFix.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoFix.Api.Services;

public class InventarioService
{
    private readonly AutoFixDbContext _db;

    public InventarioService(AutoFixDbContext db)
    {
        _db = db;
    }

    public async Task<(Repuesto Repuesto, bool AlertaStockMinimo)> UsarRepuestoAsync(
        int repuestoId,
        int cantidad,
        int? ordenReparacionId = null,
        string? usuarioEmail = null,
        string? observacion = null)
    {
        if (cantidad <= 0)
            throw new BusinessException("La cantidad debe ser mayor que cero.");

        using var tx = await _db.Database.BeginTransactionAsync();

        var repuesto = await _db.Repuestos.FindAsync(repuestoId)
            ?? throw new NotFoundException("El repuesto no existe.");

        if (repuesto.Existencias < cantidad)
            throw new BusinessException($"Existencias insuficientes para '{repuesto.Nombre}'. Disponibles: {repuesto.Existencias}, solicitadas: {cantidad}.");

        repuesto.Existencias -= cantidad;
        _db.Movimientos.Add(new MovimientoInventario(
            repuestoId,
            cantidad,
            "Salida",
            observacion ?? (ordenReparacionId.HasValue ? $"Usado en orden #{ordenReparacionId}" : "Salida de inventario"),
            ordenReparacionId,
            usuarioEmail));

        try
        {
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync();
            throw new ConflictException("Otro usuario modificó el repuesto. Intenta de nuevo.");
        }

        return (repuesto, repuesto.Existencias <= repuesto.StockMinimo);
    }

    public async Task<Repuesto> RegistrarEntradaAsync(
        int repuestoId,
        int cantidad,
        string? usuarioEmail = null,
        string? observacion = null)
    {
        if (cantidad <= 0)
            throw new BusinessException("La cantidad de entrada debe ser mayor que cero.");

        using var tx = await _db.Database.BeginTransactionAsync();

        var repuesto = await _db.Repuestos.FindAsync(repuestoId)
            ?? throw new NotFoundException("El repuesto no existe.");

        repuesto.Existencias += cantidad;
        _db.Movimientos.Add(new MovimientoInventario(
            repuestoId,
            cantidad,
            "Entrada",
            observacion ?? "Entrada de inventario",
            null,
            usuarioEmail));

        try
        {
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync();
            throw new ConflictException("Otro usuario modificó el repuesto al registrar la entrada. Intenta de nuevo.");
        }

        return repuesto;
    }

    public async Task DescontarParaPresupuestoAsync(Presupuesto presupuesto, string? usuarioEmail)
    {
        foreach (var item in presupuesto.Repuestos)
        {
            var repuesto = await _db.Repuestos.FindAsync(item.RepuestoId)
                ?? throw new NotFoundException($"El repuesto con Id {item.RepuestoId} no existe.");

            if (repuesto.Existencias < item.Cantidad)
                throw new BusinessException($"Existencias insuficientes para '{repuesto.Nombre}'. Disponibles: {repuesto.Existencias}, requeridas: {item.Cantidad}.");

            repuesto.Existencias -= item.Cantidad;
            _db.Movimientos.Add(new MovimientoInventario(
                repuesto.Id,
                item.Cantidad,
                "Salida",
                $"Descuento automático por aprobación de presupuesto #{presupuesto.Id}",
                presupuesto.OrdenReparacionId,
                usuarioEmail));
        }
    }
}
