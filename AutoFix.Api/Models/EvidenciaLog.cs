using System.ComponentModel.DataAnnotations;

namespace AutoFix.Api.Models;

public class Evidencia
{
    public int Id { get; set; }

    public int OrdenReparacionId { get; set; }
    public OrdenReparacion OrdenReparacion { get; set; } = null!;

    [Required, MaxLength(50)]
    public string Categoria { get; set; } = CategoriasEvidencia.DanioIdentificado;

    [Required, MaxLength(300)]
    public string Ruta { get; set; } = "";

    [MaxLength(300)]
    public string? Descripcion { get; set; }

    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    [MaxLength(150)]
    public string? SubidoPor { get; set; }
}

public class LogRegistro
{
    public int Id { get; set; }

    [MaxLength(150)]
    public string Usuario { get; set; } = "Anonimo";

    [MaxLength(10)]
    public string MetodoHttp { get; set; } = "";

    [MaxLength(300)]
    public string Endpoint { get; set; } = "";

    public DateTime FechaHora { get; set; } = DateTime.UtcNow;

    public int CodigoHttp { get; set; }

    public long DuracionMs { get; set; }

    [MaxLength(100)]
    public string CorrelationId { get; set; } = "";

    public string? Excepcion { get; set; }
}
