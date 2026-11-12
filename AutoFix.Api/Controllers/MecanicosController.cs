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
public class MecanicosController : ControllerBase
{
    private readonly AutoFixDbContext _db;

    public MecanicosController(AutoFixDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<MecanicoDto>>> GetAll()
    {
        var list = await _db.Mecanicos
            .Include(m => m.Asignaciones.Where(a => a.Activa))
                .ThenInclude(a => a.OrdenReparacion)
            .AsNoTracking()
            .OrderBy(m => m.NombreCompleto)
            .Select(m => new MecanicoDto
            {
                Id = m.Id,
                NombreCompleto = m.NombreCompleto,
                Especialidad = m.Especialidad,
                Telefono = m.Telefono,
                Email = m.Email,
                Activo = m.Activo,
                Version = m.Version,
                Asignaciones = m.Asignaciones.Select(a => new AsignacionResumenDto
                {
                    Id = a.Id,
                    OrdenReparacionId = a.OrdenReparacionId,
                    NumeroOrden = a.OrdenReparacion.NumeroOrden,
                    MecanicoId = a.MecanicoId,
                    MecanicoNombre = m.NombreCompleto,
                    FechaInicio = a.FechaInicio,
                    FechaFin = a.FechaFin,
                    Activa = a.Activa
                }).OrderBy(a => a.FechaInicio).ToList()
            }).ToListAsync();

        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MecanicoDto>> GetById(int id)
    {
        var m = await _db.Mecanicos
            .Include(x => x.Asignaciones.Where(a => a.Activa))
                .ThenInclude(a => a.OrdenReparacion)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException($"No existe el mecánico con Id {id}.");

        return Ok(new MecanicoDto
        {
            Id = m.Id,
            NombreCompleto = m.NombreCompleto,
            Especialidad = m.Especialidad,
            Telefono = m.Telefono,
            Email = m.Email,
            Activo = m.Activo,
            Version = m.Version,
            Asignaciones = m.Asignaciones.Select(a => new AsignacionResumenDto
            {
                Id = a.Id,
                OrdenReparacionId = a.OrdenReparacionId,
                NumeroOrden = a.OrdenReparacion.NumeroOrden,
                MecanicoId = a.MecanicoId,
                MecanicoNombre = m.NombreCompleto,
                FechaInicio = a.FechaInicio,
                FechaFin = a.FechaFin,
                Activa = a.Activa
            }).OrderBy(a => a.FechaInicio).ToList()
        });
    }

    [HttpPost]
    [Authorize(Roles = $"{RolesSistema.Administrador},{RolesSistema.Supervisor}")]
    public async Task<ActionResult<MecanicoDto>> Create(MecanicoCreateUpdateDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _db.Mecanicos.AnyAsync(m => m.Email.ToLower() == email))
            throw new ConflictException($"Ya existe un mecánico con el correo {email}.");

        var mecanico = new Mecanico
        {
            NombreCompleto = dto.NombreCompleto.Trim(),
            Especialidad = dto.Especialidad.Trim(),
            Telefono = dto.Telefono.Trim(),
            Email = email,
            Activo = dto.Activo
        };

        _db.Mecanicos.Add(mecanico);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = mecanico.Id }, new MecanicoDto
        {
            Id = mecanico.Id,
            NombreCompleto = mecanico.NombreCompleto,
            Especialidad = mecanico.Especialidad,
            Telefono = mecanico.Telefono,
            Email = mecanico.Email,
            Activo = mecanico.Activo,
            Version = mecanico.Version
        });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{RolesSistema.Administrador},{RolesSistema.Supervisor}")]
    public async Task<ActionResult<MecanicoDto>> Update(int id, MecanicoCreateUpdateDto dto)
    {
        var mecanico = await _db.Mecanicos.FindAsync(id)
            ?? throw new NotFoundException($"No existe el mecánico con Id {id}.");

        mecanico.NombreCompleto = dto.NombreCompleto.Trim();
        mecanico.Especialidad = dto.Especialidad.Trim();
        mecanico.Telefono = dto.Telefono.Trim();
        mecanico.Email = dto.Email.Trim().ToLowerInvariant();
        mecanico.Activo = dto.Activo;

        await _db.SaveChangesAsync();

        return Ok(new MecanicoDto
        {
            Id = mecanico.Id,
            NombreCompleto = mecanico.NombreCompleto,
            Especialidad = mecanico.Especialidad,
            Telefono = mecanico.Telefono,
            Email = mecanico.Email,
            Activo = mecanico.Activo,
            Version = mecanico.Version
        });
    }
}
