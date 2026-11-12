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
public class VehiculosController : ControllerBase
{
    private readonly AutoFixDbContext _db;
    private readonly ImagenStorageService _imagenes;

    public VehiculosController(AutoFixDbContext db, ImagenStorageService imagenes)
    {
        _db = db;
        _imagenes = imagenes;
    }

    [HttpGet]
    public async Task<ActionResult<List<VehiculoDto>>> GetAll([FromQuery] int? clienteId = null)
    {
        var query = _db.Vehiculos.Include(v => v.Cliente).AsNoTracking().AsQueryable();

        if (User.IsInRole(RolesSistema.Cliente))
        {
            var cidStr = User.FindFirstValue("clienteId");
            if (!int.TryParse(cidStr, out var cid))
                return Forbid();
            query = query.Where(v => v.ClienteId == cid);
        }
        else if (clienteId.HasValue)
        {
            query = query.Where(v => v.ClienteId == clienteId.Value);
        }

        var list = await query.OrderBy(v => v.Placa).Select(v => new VehiculoDto
        {
            Id = v.Id,
            Placa = v.Placa,
            Marca = v.Marca,
            Modelo = v.Modelo,
            Anio = v.Anio,
            Color = v.Color,
            NumeroChasis = v.NumeroChasis,
            FotografiaRuta = v.FotografiaRuta,
            ClienteId = v.ClienteId,
            ClienteNombre = v.Cliente.NombreCompleto
        }).ToListAsync();

        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VehiculoDto>> GetById(int id)
    {
        var v = await _db.Vehiculos.Include(x => x.Cliente).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException($"No existe el vehículo con Id {id}.");

        if (User.IsInRole(RolesSistema.Cliente))
        {
            var cidStr = User.FindFirstValue("clienteId");
            if (!int.TryParse(cidStr, out var cid) || v.ClienteId != cid)
                return Forbid();
        }

        return Ok(new VehiculoDto
        {
            Id = v.Id,
            Placa = v.Placa,
            Marca = v.Marca,
            Modelo = v.Modelo,
            Anio = v.Anio,
            Color = v.Color,
            NumeroChasis = v.NumeroChasis,
            FotografiaRuta = v.FotografiaRuta,
            ClienteId = v.ClienteId,
            ClienteNombre = v.Cliente.NombreCompleto
        });
    }

    [HttpPost]
    [Authorize(Roles = $"{RolesSistema.Administrador},{RolesSistema.Recepcionista}")]
    public async Task<ActionResult<VehiculoDto>> Create(VehiculoCreateUpdateDto dto)
    {
        var placa = dto.Placa.Trim().ToUpperInvariant();
        if (await _db.Vehiculos.AnyAsync(v => v.Placa == placa))
            throw new ConflictException($"Ya existe un vehículo registrado con la placa {placa}.");

        var cliente = await _db.Clientes.FindAsync(dto.ClienteId)
            ?? throw new NotFoundException($"No existe el cliente con Id {dto.ClienteId}.");

        var vehiculo = new Vehiculo
        {
            Placa = placa,
            Marca = dto.Marca.Trim(),
            Modelo = dto.Modelo.Trim(),
            Anio = dto.Anio,
            Color = dto.Color.Trim(),
            NumeroChasis = dto.NumeroChasis.Trim().ToUpperInvariant(),
            ClienteId = cliente.Id
        };

        _db.Vehiculos.Add(vehiculo);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = vehiculo.Id }, new VehiculoDto
        {
            Id = vehiculo.Id,
            Placa = vehiculo.Placa,
            Marca = vehiculo.Marca,
            Modelo = vehiculo.Modelo,
            Anio = vehiculo.Anio,
            Color = vehiculo.Color,
            NumeroChasis = vehiculo.NumeroChasis,
            FotografiaRuta = vehiculo.FotografiaRuta,
            ClienteId = vehiculo.ClienteId,
            ClienteNombre = cliente.NombreCompleto
        });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{RolesSistema.Administrador},{RolesSistema.Recepcionista}")]
    public async Task<ActionResult<VehiculoDto>> Update(int id, VehiculoCreateUpdateDto dto)
    {
        var vehiculo = await _db.Vehiculos.FindAsync(id)
            ?? throw new NotFoundException($"No existe el vehículo con Id {id}.");

        var placa = dto.Placa.Trim().ToUpperInvariant();
        if (await _db.Vehiculos.AnyAsync(v => v.Placa == placa && v.Id != id))
            throw new ConflictException($"Ya existe otro vehículo con la placa {placa}.");

        var cliente = await _db.Clientes.FindAsync(dto.ClienteId)
            ?? throw new NotFoundException($"No existe el cliente con Id {dto.ClienteId}.");

        vehiculo.Placa = placa;
        vehiculo.Marca = dto.Marca.Trim();
        vehiculo.Modelo = dto.Modelo.Trim();
        vehiculo.Anio = dto.Anio;
        vehiculo.Color = dto.Color.Trim();
        vehiculo.NumeroChasis = dto.NumeroChasis.Trim().ToUpperInvariant();
        vehiculo.ClienteId = cliente.Id;

        await _db.SaveChangesAsync();

        return Ok(new VehiculoDto
        {
            Id = vehiculo.Id,
            Placa = vehiculo.Placa,
            Marca = vehiculo.Marca,
            Modelo = vehiculo.Modelo,
            Anio = vehiculo.Anio,
            Color = vehiculo.Color,
            NumeroChasis = vehiculo.NumeroChasis,
            FotografiaRuta = vehiculo.FotografiaRuta,
            ClienteId = vehiculo.ClienteId,
            ClienteNombre = cliente.NombreCompleto
        });
    }

    [HttpPost("{id:int}/foto")]
    [Authorize(Roles = $"{RolesSistema.Administrador},{RolesSistema.Recepcionista}")]
    public async Task<IActionResult> SubirFoto(int id, IFormFile archivo)
    {
        var vehiculo = await _db.Vehiculos.FindAsync(id)
            ?? throw new NotFoundException($"No existe el vehículo con Id {id}.");

        var ruta = await _imagenes.GuardarImagenAsync(archivo);
        vehiculo.FotografiaRuta = ruta;
        await _db.SaveChangesAsync();

        return Ok(new { ruta });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = RolesSistema.Administrador)]
    public async Task<IActionResult> Delete(int id)
    {
        var vehiculo = await _db.Vehiculos.Include(v => v.Ordenes).FirstOrDefaultAsync(v => v.Id == id)
            ?? throw new NotFoundException($"No existe el vehículo con Id {id}.");

        if (vehiculo.Ordenes.Count > 0)
            throw new BusinessException("No se puede eliminar un vehículo que tiene órdenes de reparación registradas.");

        _db.Vehiculos.Remove(vehiculo);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
