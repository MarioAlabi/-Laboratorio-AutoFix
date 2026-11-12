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
public class ClientesController : ControllerBase
{
    private readonly AutoFixDbContext _db;

    public ClientesController(AutoFixDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<ClienteDto>>> GetAll()
    {
        var query = _db.Clientes.Include(c => c.Vehiculos).AsNoTracking().AsQueryable();

        if (User.IsInRole(RolesSistema.Cliente))
        {
            var clienteIdStr = User.FindFirstValue("clienteId");
            if (!int.TryParse(clienteIdStr, out var clienteId))
                return Forbid();
            query = query.Where(c => c.Id == clienteId);
        }

        var list = await query.OrderBy(c => c.NombreCompleto).Select(c => new ClienteDto
        {
            Id = c.Id,
            NombreCompleto = c.NombreCompleto,
            Dui = c.Dui,
            Telefono = c.Telefono,
            CorreoElectronico = c.CorreoElectronico,
            Direccion = c.Direccion,
            Vehiculos = c.Vehiculos.Select(v => new VehiculoDto
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
                ClienteNombre = c.NombreCompleto
            }).ToList()
        }).ToListAsync();

        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClienteDto>> GetById(int id)
    {
        if (User.IsInRole(RolesSistema.Cliente))
        {
            var clienteIdStr = User.FindFirstValue("clienteId");
            if (!int.TryParse(clienteIdStr, out var cid) || cid != id)
                return Forbid();
        }

        var c = await _db.Clientes.Include(x => x.Vehiculos).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException($"No existe el cliente con Id {id}.");

        return Ok(new ClienteDto
        {
            Id = c.Id,
            NombreCompleto = c.NombreCompleto,
            Dui = c.Dui,
            Telefono = c.Telefono,
            CorreoElectronico = c.CorreoElectronico,
            Direccion = c.Direccion,
            Vehiculos = c.Vehiculos.Select(v => new VehiculoDto
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
                ClienteNombre = c.NombreCompleto
            }).ToList()
        });
    }

    [HttpPost]
    [Authorize(Roles = $"{RolesSistema.Administrador},{RolesSistema.Recepcionista}")]
    public async Task<ActionResult<ClienteDto>> Create(ClienteCreateUpdateDto dto)
    {
        var dui = dto.Dui.Trim();
        if (await _db.Clientes.AnyAsync(c => c.Dui == dui))
            throw new ConflictException($"Ya existe un cliente registrado con el DUI {dui}.");

        var cliente = new Cliente
        {
            NombreCompleto = dto.NombreCompleto.Trim(),
            Dui = dui,
            Telefono = dto.Telefono.Trim(),
            CorreoElectronico = dto.CorreoElectronico.Trim(),
            Direccion = dto.Direccion.Trim()
        };

        _db.Clientes.Add(cliente);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = cliente.Id }, new ClienteDto
        {
            Id = cliente.Id,
            NombreCompleto = cliente.NombreCompleto,
            Dui = cliente.Dui,
            Telefono = cliente.Telefono,
            CorreoElectronico = cliente.CorreoElectronico,
            Direccion = cliente.Direccion
        });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{RolesSistema.Administrador},{RolesSistema.Recepcionista}")]
    public async Task<ActionResult<ClienteDto>> Update(int id, ClienteCreateUpdateDto dto)
    {
        var cliente = await _db.Clientes.FindAsync(id)
            ?? throw new NotFoundException($"No existe el cliente con Id {id}.");

        var dui = dto.Dui.Trim();
        if (await _db.Clientes.AnyAsync(c => c.Dui == dui && c.Id != id))
            throw new ConflictException($"Ya existe otro cliente con el DUI {dui}.");

        cliente.NombreCompleto = dto.NombreCompleto.Trim();
        cliente.Dui = dui;
        cliente.Telefono = dto.Telefono.Trim();
        cliente.CorreoElectronico = dto.CorreoElectronico.Trim();
        cliente.Direccion = dto.Direccion.Trim();

        await _db.SaveChangesAsync();

        return Ok(new ClienteDto
        {
            Id = cliente.Id,
            NombreCompleto = cliente.NombreCompleto,
            Dui = cliente.Dui,
            Telefono = cliente.Telefono,
            CorreoElectronico = cliente.CorreoElectronico,
            Direccion = cliente.Direccion
        });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = RolesSistema.Administrador)]
    public async Task<IActionResult> Delete(int id)
    {
        var cliente = await _db.Clientes
            .Include(c => c.Vehiculos)
            .Include(c => c.Ordenes)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new NotFoundException($"No existe el cliente con Id {id}.");

        if (cliente.Vehiculos.Count > 0 || cliente.Ordenes.Count > 0)
            throw new BusinessException("No se puede eliminar un cliente que tiene vehículos u órdenes asociadas.");

        _db.Clientes.Remove(cliente);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
