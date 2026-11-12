using System.ComponentModel.DataAnnotations;

namespace AutoFix.Api.DTOs;

public class ClienteDto
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = "";
    public string Dui { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string CorreoElectronico { get; set; } = "";
    public string Direccion { get; set; } = "";
    public List<VehiculoDto> Vehiculos { get; set; } = [];
}

public class ClienteCreateUpdateDto
{
    [Required, MaxLength(150)]
    public string NombreCompleto { get; set; } = "";

    [Required, MaxLength(30)]
    public string Dui { get; set; } = "";

    [Required, MaxLength(30)]
    public string Telefono { get; set; } = "";

    [Required, EmailAddress, MaxLength(150)]
    public string CorreoElectronico { get; set; } = "";

    [Required, MaxLength(250)]
    public string Direccion { get; set; } = "";
}

public class VehiculoDto
{
    public int Id { get; set; }
    public string Placa { get; set; } = "";
    public string Marca { get; set; } = "";
    public string Modelo { get; set; } = "";
    public int Anio { get; set; }
    public string Color { get; set; } = "";
    public string NumeroChasis { get; set; } = "";
    public string? FotografiaRuta { get; set; }
    public int ClienteId { get; set; }
    public string ClienteNombre { get; set; } = "";
}

public class VehiculoCreateUpdateDto
{
    [Required, MaxLength(20)]
    public string Placa { get; set; } = "";

    [Required, MaxLength(100)]
    public string Marca { get; set; } = "";

    [Required, MaxLength(100)]
    public string Modelo { get; set; } = "";

    [Range(1950, 2100)]
    public int Anio { get; set; }

    [Required, MaxLength(50)]
    public string Color { get; set; } = "";

    [Required, MaxLength(60)]
    public string NumeroChasis { get; set; } = "";

    [Required]
    public int ClienteId { get; set; }
}

public class MecanicoDto
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = "";
    public string Especialidad { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string Email { get; set; } = "";
    public bool Activo { get; set; }
    public Guid Version { get; set; }
    public List<AsignacionResumenDto> Asignaciones { get; set; } = [];
}

public class MecanicoCreateUpdateDto
{
    [Required, MaxLength(150)]
    public string NombreCompleto { get; set; } = "";

    [Required, MaxLength(100)]
    public string Especialidad { get; set; } = "";

    [Required, MaxLength(30)]
    public string Telefono { get; set; } = "";

    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; set; } = "";

    public bool Activo { get; set; } = true;
}

public class AsignacionResumenDto
{
    public int Id { get; set; }
    public int OrdenReparacionId { get; set; }
    public string NumeroOrden { get; set; } = "";
    public int MecanicoId { get; set; }
    public string MecanicoNombre { get; set; } = "";
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public bool Activa { get; set; }
}
