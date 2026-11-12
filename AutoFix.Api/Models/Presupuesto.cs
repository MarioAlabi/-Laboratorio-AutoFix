using System.ComponentModel.DataAnnotations;

namespace AutoFix.Api.Models;

public class Presupuesto
{
    public int Id { get; set; }

    public int OrdenReparacionId { get; set; }
    public OrdenReparacion OrdenReparacion { get; set; } = null!;

    public decimal ManoDeObra { get; set; }
    public decimal Total { get; set; }

    [MaxLength(500)]
    public string? Observaciones { get; set; }

    public bool Aprobado { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaAprobacion { get; set; }

    [MaxLength(150)]
    public string? AprobadoPor { get; set; }

    public ICollection<PresupuestoRepuesto> Repuestos { get; set; } = new List<PresupuestoRepuesto>();
}

public class PresupuestoRepuesto
{
    public int Id { get; set; }

    public int PresupuestoId { get; set; }
    public Presupuesto Presupuesto { get; set; } = null!;

    public int RepuestoId { get; set; }
    public Repuesto Repuesto { get; set; } = null!;

    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}
