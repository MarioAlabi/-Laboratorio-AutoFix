using System.ComponentModel.DataAnnotations;

namespace AutoFix.Api.Models;

public class Repuesto : EntidadConcurrente
{
    public int Id { get; set; }

    [Required, MaxLength(40)]
    public string Codigo { get; set; } = "";

    [Required, MaxLength(150)]
    public string Nombre { get; set; } = "";

    [MaxLength(400)]
    public string? Descripcion { get; set; }

    [Required, MaxLength(80)]
    public string Categoria { get; set; } = "";

    [Required, MaxLength(80)]
    public string Marca { get; set; } = "";

    public decimal Precio { get; set; }
    public int Existencias { get; set; }
    public int StockMinimo { get; set; }

    [MaxLength(300)]
    public string? FotografiaRuta { get; set; }

    public ICollection<MovimientoInventario> Movimientos { get; set; } = new List<MovimientoInventario>();
}

public class MovimientoInventario
{
    public MovimientoInventario() { }

    public MovimientoInventario(int repuestoId, int cantidad, string tipo, string? observacion = null, int? ordenReparacionId = null, string? usuarioEmail = null)
    {
        RepuestoId = repuestoId;
        Cantidad = cantidad;
        Tipo = tipo;
        Observacion = observacion;
        OrdenReparacionId = ordenReparacionId;
        UsuarioEmail = usuarioEmail;
        Fecha = DateTime.UtcNow;
    }

    public int Id { get; set; }

    public int RepuestoId { get; set; }
    public Repuesto Repuesto { get; set; } = null!;

    public int Cantidad { get; set; }

    [Required, MaxLength(20)]
    public string Tipo { get; set; } = "Salida"; // "Entrada" | "Salida"

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    [MaxLength(300)]
    public string? Observacion { get; set; }

    public int? OrdenReparacionId { get; set; }

    [MaxLength(150)]
    public string? UsuarioEmail { get; set; }
}
