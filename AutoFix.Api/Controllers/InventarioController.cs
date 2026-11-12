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
public class InventarioController : ControllerBase
{
    private readonly AutoFixDbContext _db;
    private readonly InventarioService _inventario;
    private readonly ImagenStorageService _imagenes;

    public InventarioController(AutoFixDbContext db, InventarioService inventario, ImagenStorageService imagenes)
    {
        _db = db;
        _inventario = inventario;
        _imagenes = imagenes;
    }

    [HttpGet]
    public async Task<ActionResult<List<RepuestoDto>>> GetAll([FromQuery] bool soloAlertas = false)
    {
        var query = _db.Repuestos.AsNoTracking().AsQueryable();
        if (soloAlertas)
            query = query.Where(r => r.Existencias <= r.StockMinimo);

        var list = await query.OrderBy(r => r.Nombre).Select(r => new RepuestoDto
        {
            Id = r.Id,
            Codigo = r.Codigo,
            Nombre = r.Nombre,
            Descripcion = r.Descripcion,
            Categoria = r.Categoria,
            Marca = r.Marca,
            Precio = r.Precio,
            Existencias = r.Existencias,
            StockMinimo = r.StockMinimo,
            AlertaStockMinimo = r.Existencias <= r.StockMinimo,
            FotografiaRuta = r.FotografiaRuta,
            Version = r.Version
        }).ToListAsync();

        return Ok(list);
    }

    [HttpGet("alertas")]
    public Task<ActionResult<List<RepuestoDto>>> GetAlertas() => GetAll(soloAlertas: true);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RepuestoDto>> GetById(int id)
    {
        var r = await _db.Repuestos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException($"No existe el repuesto con Id {id}.");

        return Ok(MapDto(r));
    }

    [HttpPost]
    [Authorize(Policy = "PuedeGestionarInventario")]
    public async Task<ActionResult<RepuestoDto>> Create(RepuestoCreateUpdateDto dto)
    {
        var codigo = dto.Codigo.Trim().ToUpperInvariant();
        if (await _db.Repuestos.AnyAsync(r => r.Codigo == codigo))
            throw new ConflictException($"Ya existe un repuesto con el código {codigo}.");

        var repuesto = new Repuesto
        {
            Codigo = codigo,
            Nombre = dto.Nombre.Trim(),
            Descripcion = dto.Descripcion?.Trim(),
            Categoria = dto.Categoria.Trim(),
            Marca = dto.Marca.Trim(),
            Precio = dto.Precio,
            Existencias = dto.Existencias,
            StockMinimo = dto.StockMinimo
        };

        _db.Repuestos.Add(repuesto);
        if (repuesto.Existencias > 0)
        {
            _db.Movimientos.Add(new MovimientoInventario
            {
                Repuesto = repuesto,
                Cantidad = repuesto.Existencias,
                Tipo = "Entrada",
                Observacion = "Stock inicial de registro",
                UsuarioEmail = User.Identity?.Name
            });
        }

        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = repuesto.Id }, MapDto(repuesto));
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "PuedeGestionarInventario")]
    public async Task<ActionResult<RepuestoDto>> Update(int id, RepuestoCreateUpdateDto dto)
    {
        var repuesto = await _db.Repuestos.FindAsync(id)
            ?? throw new NotFoundException($"No existe el repuesto con Id {id}.");

        var codigo = dto.Codigo.Trim().ToUpperInvariant();
        if (await _db.Repuestos.AnyAsync(r => r.Codigo == codigo && r.Id != id))
            throw new ConflictException($"Ya existe otro repuesto con el código {codigo}.");

        if (dto.Version.HasValue)
            _db.Entry(repuesto).Property(r => r.Version).OriginalValue = dto.Version.Value;

        repuesto.Codigo = codigo;
        repuesto.Nombre = dto.Nombre.Trim();
        repuesto.Descripcion = dto.Descripcion?.Trim();
        repuesto.Categoria = dto.Categoria.Trim();
        repuesto.Marca = dto.Marca.Trim();
        repuesto.Precio = dto.Precio;
        repuesto.Existencias = dto.Existencias;
        repuesto.StockMinimo = dto.StockMinimo;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("Otro usuario modificó el repuesto. Recarga los datos e intenta de nuevo.");
        }

        return Ok(MapDto(repuesto));
    }

    [HttpPost("{id:int}/foto")]
    [Authorize(Policy = "PuedeGestionarInventario")]
    public async Task<IActionResult> SubirFoto(int id, IFormFile archivo)
    {
        var repuesto = await _db.Repuestos.FindAsync(id)
            ?? throw new NotFoundException($"No existe el repuesto con Id {id}.");

        var ruta = await _imagenes.GuardarImagenAsync(archivo);
        repuesto.FotografiaRuta = ruta;
        await _db.SaveChangesAsync();

        return Ok(new { ruta });
    }

    [HttpPost("{id:int}/entrada")]
    [Authorize(Policy = "PuedeGestionarInventario")]
    public async Task<ActionResult<RepuestoDto>> RegistrarEntrada(int id, MovimientoRequestDto dto)
    {
        var repuesto = await _inventario.RegistrarEntradaAsync(id, dto.Cantidad, User.Identity?.Name, dto.Observacion);
        return Ok(MapDto(repuesto));
    }

    [HttpPost("{id:int}/usar")]
    [Authorize(Roles = $"{RolesSistema.Administrador},{RolesSistema.Supervisor},{RolesSistema.Mecanico}")]
    public async Task<IActionResult> UsarRepuesto(int id, MovimientoRequestDto dto)
    {
        if (dto.Version.HasValue)
        {
            var r = await _db.Repuestos.FindAsync(id)
                ?? throw new NotFoundException("El repuesto no existe.");
            _db.Entry(r).Property(x => x.Version).OriginalValue = dto.Version.Value;
        }

        var (repuesto, alerta) = await _inventario.UsarRepuestoAsync(
            id,
            dto.Cantidad,
            dto.OrdenReparacionId,
            User.Identity?.Name,
            dto.Observacion);

        return Ok(new
        {
            repuesto = MapDto(repuesto),
            alertaStockMinimo = alerta,
            mensaje = alerta
                ? $"Repuesto descontado. ALERTA: '{repuesto.Nombre}' alcanzó el stock mínimo ({repuesto.Existencias}/{repuesto.StockMinimo})."
                : "Repuesto descontado correctamente."
        });
    }

    private static RepuestoDto MapDto(Repuesto r) => new()
    {
        Id = r.Id,
        Codigo = r.Codigo,
        Nombre = r.Nombre,
        Descripcion = r.Descripcion,
        Categoria = r.Categoria,
        Marca = r.Marca,
        Precio = r.Precio,
        Existencias = r.Existencias,
        StockMinimo = r.StockMinimo,
        AlertaStockMinimo = r.Existencias <= r.StockMinimo,
        FotografiaRuta = r.FotografiaRuta,
        Version = r.Version
    };
}
