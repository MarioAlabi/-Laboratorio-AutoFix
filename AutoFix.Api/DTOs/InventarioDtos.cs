using System.ComponentModel.DataAnnotations;

namespace AutoFix.Api.DTOs;

public class RepuestoDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public string Categoria { get; set; } = "";
    public string Marca { get; set; } = "";
    public decimal Precio { get; set; }
    public int Existencias { get; set; }
    public int StockMinimo { get; set; }
    public bool AlertaStockMinimo { get; set; }
    public string? FotografiaRuta { get; set; }
    public Guid Version { get; set; }
}

public class RepuestoCreateUpdateDto
{
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

    [Range(0.01, 1000000)]
    public decimal Precio { get; set; }

    [Range(0, 1000000)]
    public int Existencias { get; set; }

    [Range(0, 1000000)]
    public int StockMinimo { get; set; }

    public Guid? Version { get; set; }
}

public class MovimientoRequestDto
{
    [Range(1, 100000)]
    public int Cantidad { get; set; }

    public int? OrdenReparacionId { get; set; }

    [MaxLength(300)]
    public string? Observacion { get; set; }

    public Guid? Version { get; set; }
}

public class MovimientoDto
{
    public int Id { get; set; }
    public int RepuestoId { get; set; }
    public string RepuestoCodigo { get; set; } = "";
    public string RepuestoNombre { get; set; } = "";
    public int Cantidad { get; set; }
    public string Tipo { get; set; } = "";
    public DateTime Fecha { get; set; }
    public string? Observacion { get; set; }
    public int? OrdenReparacionId { get; set; }
    public string? UsuarioEmail { get; set; }
}

public class AsignarMecanicoRequestDto
{
    [Required]
    public int OrdenReparacionId { get; set; }

    [Required]
    public int MecanicoId { get; set; }

    [Required]
    public DateTime FechaInicio { get; set; }

    [Required]
    public DateTime FechaFin { get; set; }

    public Guid? MecanicoVersion { get; set; }
}
