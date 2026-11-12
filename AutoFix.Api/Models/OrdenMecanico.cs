using System.ComponentModel.DataAnnotations;

namespace AutoFix.Api.Models;

public class Mecanico : EntidadConcurrente
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string NombreCompleto { get; set; } = "";

    [Required, MaxLength(100)]
    public string Especialidad { get; set; } = "";

    [Required, MaxLength(30)]
    public string Telefono { get; set; } = "";

    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; set; } = "";

    public bool Activo { get; set; } = true;

    public ICollection<OrdenReparacion> Ordenes { get; set; } = new List<OrdenReparacion>();
    public ICollection<AsignacionMecanico> Asignaciones { get; set; } = new List<AsignacionMecanico>();
}

public class OrdenReparacion : EntidadConcurrente
{
    public int Id { get; set; }

    [Required, MaxLength(30)]
    public string NumeroOrden { get; set; } = "";

    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;

    public int VehiculoId { get; set; }
    public Vehiculo Vehiculo { get; set; } = null!;

    public DateTime FechaIngreso { get; set; } = DateTime.UtcNow;
    public DateTime? FechaEstimadaFinalizacion { get; set; }

    [Required, MaxLength(500)]
    public string DescripcionProblema { get; set; } = "";

    [MaxLength(1000)]
    public string? Diagnostico { get; set; }

    public int? MecanicoId { get; set; }
    public Mecanico? Mecanico { get; set; }

    public EstadoOrden Estado { get; set; } = EstadoOrden.Recibido;

    public Presupuesto? Presupuesto { get; set; }
    public ICollection<Evidencia> Evidencias { get; set; } = new List<Evidencia>();
    public ICollection<AsignacionMecanico> Asignaciones { get; set; } = new List<AsignacionMecanico>();
}

public class AsignacionMecanico
{
    public int Id { get; set; }

    public int OrdenReparacionId { get; set; }
    public OrdenReparacion OrdenReparacion { get; set; } = null!;

    public int MecanicoId { get; set; }
    public Mecanico Mecanico { get; set; } = null!;

    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public bool Activa { get; set; } = true;
    public DateTime FechaAsignacion { get; set; } = DateTime.UtcNow;
}
