using System.ComponentModel.DataAnnotations;
using AutoFix.Api.Models;

namespace AutoFix.Api.DTOs;

public class OrdenDto
{
    public int Id { get; set; }
    public string NumeroOrden { get; set; } = "";
    public int ClienteId { get; set; }
    public string ClienteNombre { get; set; } = "";
    public int VehiculoId { get; set; }
    public string VehiculoPlaca { get; set; } = "";
    public string VehiculoDescripcion { get; set; } = "";
    public DateTime FechaIngreso { get; set; }
    public DateTime? FechaEstimadaFinalizacion { get; set; }
    public string DescripcionProblema { get; set; } = "";
    public string? Diagnostico { get; set; }
    public int? MecanicoId { get; set; }
    public string? MecanicoNombre { get; set; }
    public EstadoOrden Estado { get; set; }
    public string EstadoNombre { get; set; } = "";
    public bool TienePresupuesto { get; set; }
    public bool PresupuestoAprobado { get; set; }
    public int CantidadEvidencias { get; set; }
}

public class OrdenDetalleDto : OrdenDto
{
    public PresupuestoDto? Presupuesto { get; set; }
    public List<EvidenciaDto> Evidencias { get; set; } = [];
    public List<AsignacionResumenDto> Asignaciones { get; set; } = [];
}

public class OrdenCreateDto
{
    [Required]
    public int ClienteId { get; set; }

    [Required]
    public int VehiculoId { get; set; }

    public DateTime? FechaEstimadaFinalizacion { get; set; }

    [Required, MaxLength(500)]
    public string DescripcionProblema { get; set; } = "";
}

public class OrdenUpdateDto
{
    public DateTime? FechaEstimadaFinalizacion { get; set; }

    [Required, MaxLength(500)]
    public string DescripcionProblema { get; set; } = "";

    [MaxLength(1000)]
    public string? Diagnostico { get; set; }
}

public class CambiarEstadoDto
{
    [Required]
    public EstadoOrden NuevoEstado { get; set; }
}

public class PresupuestoItemCreateDto
{
    [Required]
    public int RepuestoId { get; set; }

    [Range(1, 10000)]
    public int Cantidad { get; set; }
}

public class PresupuestoCreateDto
{
    [Required]
    public int OrdenReparacionId { get; set; }

    [Range(0, 1000000)]
    public decimal ManoDeObra { get; set; }

    [MaxLength(500)]
    public string? Observaciones { get; set; }

    public bool EnviarAAprobacion { get; set; } = true;

    public List<PresupuestoItemCreateDto> Repuestos { get; set; } = [];
}

public class PresupuestoRepuestoDto
{
    public int Id { get; set; }
    public int RepuestoId { get; set; }
    public string RepuestoCodigo { get; set; } = "";
    public string RepuestoNombre { get; set; } = "";
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}

public class PresupuestoDto
{
    public int Id { get; set; }
    public int OrdenReparacionId { get; set; }
    public string NumeroOrden { get; set; } = "";
    public string VehiculoPlaca { get; set; } = "";
    public string ClienteNombre { get; set; } = "";
    public decimal ManoDeObra { get; set; }
    public decimal Total { get; set; }
    public string? Observaciones { get; set; }
    public bool Aprobado { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaAprobacion { get; set; }
    public string? AprobadoPor { get; set; }
    public List<PresupuestoRepuestoDto> Repuestos { get; set; } = [];
}

public class EvidenciaDto
{
    public int Id { get; set; }
    public int OrdenReparacionId { get; set; }
    public string Categoria { get; set; } = "";
    public string Ruta { get; set; } = "";
    public string? Descripcion { get; set; }
    public DateTime FechaRegistro { get; set; }
    public string? SubidoPor { get; set; }
}
