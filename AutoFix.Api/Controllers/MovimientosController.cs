using AutoFix.Api.Data;
using AutoFix.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoFix.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MovimientosController : ControllerBase
{
    private readonly AutoFixDbContext _db;

    public MovimientosController(AutoFixDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<MovimientoDto>>> GetAll([FromQuery] int? repuestoId = null)
    {
        var query = _db.Movimientos
            .Include(m => m.Repuesto)
            .AsNoTracking()
            .AsQueryable();

        if (repuestoId.HasValue)
            query = query.Where(m => m.RepuestoId == repuestoId.Value);

        var list = await query
            .OrderByDescending(m => m.Fecha)
            .Select(m => new MovimientoDto
            {
                Id = m.Id,
                RepuestoId = m.RepuestoId,
                RepuestoCodigo = m.Repuesto.Codigo,
                RepuestoNombre = m.Repuesto.Nombre,
                Cantidad = m.Cantidad,
                Tipo = m.Tipo,
                Fecha = m.Fecha,
                Observacion = m.Observacion,
                OrdenReparacionId = m.OrdenReparacionId,
                UsuarioEmail = m.UsuarioEmail
            })
            .ToListAsync();

        return Ok(list);
    }
}
