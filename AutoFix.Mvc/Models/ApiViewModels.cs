using System.ComponentModel.DataAnnotations;

namespace AutoFix.Mvc.Models;

public class LoginVm
{
    [Required(ErrorMessage = "El correo es obligatorio"), EmailAddress]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    public string Password { get; set; } = "";
}

public class RegisterVm
{
    [Required, MaxLength(150)]
    public string NombreCompleto { get; set; } = "";

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, MinLength(6)]
    public string Password { get; set; } = "";

    public string Rol { get; set; } = "Cliente";
}

public class AuthResponseVm
{
    public string Token { get; set; } = "";
    public DateTime Expiracion { get; set; }
    public int UsuarioId { get; set; }
    public string NombreCompleto { get; set; } = "";
    public string Email { get; set; } = "";
    public string Rol { get; set; } = "";
    public List<string> Claims { get; set; } = [];
    public int? ClienteId { get; set; }
    public int? MecanicoId { get; set; }
}

public class DashboardVm
{
    public int TotalOrdenes { get; set; }
    public int OrdenesActivas { get; set; }
    public int TotalClientes { get; set; }
    public int TotalVehiculos { get; set; }
    public int TotalMecanicos { get; set; }
    public int AlertasStockMinimo { get; set; }
    public List<EstadoConteoVm> OrdenesPorEstado { get; set; } = [];
}

public class EstadoConteoVm
{
    public string Estado { get; set; } = "";
    public string EstadoNombre { get; set; } = "";
    public int Cantidad { get; set; }
}

public class ClienteVm
{
    public int Id { get; set; }
    [Required, MaxLength(150)]
    public string NombreCompleto { get; set; } = "";
    [Required, MaxLength(30)]
    public string Dui { get; set; } = "";
    [Required, MaxLength(30)]
    public string Telefono { get; set; } = "";
    [Required, EmailAddress]
    public string CorreoElectronico { get; set; } = "";
    [Required, MaxLength(250)]
    public string Direccion { get; set; } = "";
    public List<VehiculoVm> Vehiculos { get; set; } = [];
}

public class VehiculoVm
{
    public int Id { get; set; }
    [Required, MaxLength(20)]
    public string Placa { get; set; } = "";
    [Required, MaxLength(100)]
    public string Marca { get; set; } = "";
    [Required, MaxLength(100)]
    public string Modelo { get; set; } = "";
    [Range(1950, 2100)]
    public int Anio { get; set; } = DateTime.Now.Year;
    [Required, MaxLength(50)]
    public string Color { get; set; } = "";
    [Required, MaxLength(60)]
    public string NumeroChasis { get; set; } = "";
    public string? FotografiaRuta { get; set; }
    [Required]
    public int ClienteId { get; set; }
    public string ClienteNombre { get; set; } = "";
}

public class MecanicoVm
{
    public int Id { get; set; }
    [Required, MaxLength(150)]
    public string NombreCompleto { get; set; } = "";
    [Required, MaxLength(100)]
    public string Especialidad { get; set; } = "";
    [Required, MaxLength(30)]
    public string Telefono { get; set; } = "";
    [Required, EmailAddress]
    public string Email { get; set; } = "";
    public bool Activo { get; set; } = true;
    public Guid Version { get; set; }
    public List<AsignacionVm> Asignaciones { get; set; } = [];
}

public class AsignacionVm
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

public class AsignarMecanicoVm
{
    [Required]
    public int OrdenReparacionId { get; set; }
    [Required]
    public int MecanicoId { get; set; }
    [Required]
    public DateTime FechaInicio { get; set; } = DateTime.Now;
    [Required]
    public DateTime FechaFin { get; set; } = DateTime.Now.AddHours(2);
    public Guid? MecanicoVersion { get; set; }
}

public class OrdenVm
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
    public int Estado { get; set; }
    public string EstadoNombre { get; set; } = "";
    public bool TienePresupuesto { get; set; }
    public bool PresupuestoAprobado { get; set; }
    public int CantidadEvidencias { get; set; }
}

public class OrdenDetalleVm : OrdenVm
{
    public PresupuestoVm? Presupuesto { get; set; }
    public List<EvidenciaVm> Evidencias { get; set; } = [];
    public List<AsignacionVm> Asignaciones { get; set; } = [];
}

public class OrdenCreateVm
{
    [Required]
    public int ClienteId { get; set; }
    [Required]
    public int VehiculoId { get; set; }
    public DateTime? FechaEstimadaFinalizacion { get; set; }
    [Required, MaxLength(500)]
    public string DescripcionProblema { get; set; } = "";
}

public class PresupuestoRepuestoVm
{
    public int Id { get; set; }
    public int RepuestoId { get; set; }
    public string RepuestoCodigo { get; set; } = "";
    public string RepuestoNombre { get; set; } = "";
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}

public class PresupuestoVm
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
    public List<PresupuestoRepuestoVm> Repuestos { get; set; } = [];
}

public class PresupuestoCreateVm
{
    [Required]
    public int OrdenReparacionId { get; set; }
    [Range(0, 1000000)]
    public decimal ManoDeObra { get; set; }
    [MaxLength(500)]
    public string? Observaciones { get; set; }
    public bool EnviarAAprobacion { get; set; } = true;
    public List<PresupuestoItemVm> Repuestos { get; set; } = [];
}

public class PresupuestoItemVm
{
    public int RepuestoId { get; set; }
    public int Cantidad { get; set; }
}

public class RepuestoVm
{
    public int Id { get; set; }
    [Required, MaxLength(40)]
    public string Codigo { get; set; } = "";
    [Required, MaxLength(150)]
    public string Nombre { get; set; } = "";
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
    public bool AlertaStockMinimo { get; set; }
    public string? FotografiaRuta { get; set; }
    public Guid Version { get; set; }
}

public class MovimientoVm
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

public class MovimientoCrearVm
{
    [Required]
    public int RepuestoId { get; set; }
    [Range(1, 100000)]
    public int Cantidad { get; set; } = 1;
    [Required]
    public string Tipo { get; set; } = "Salida";
    public int? OrdenReparacionId { get; set; }
    public string? Observacion { get; set; }
    public Guid? Version { get; set; }
}

public class EvidenciaVm
{
    public int Id { get; set; }
    public int OrdenReparacionId { get; set; }
    public string Categoria { get; set; } = "";
    public string Ruta { get; set; } = "";
    public string? Descripcion { get; set; }
    public DateTime FechaRegistro { get; set; }
    public string? SubidoPor { get; set; }
}

public class LogRegistroVm
{
    public int Id { get; set; }
    public string Usuario { get; set; } = "";
    public string MetodoHttp { get; set; } = "";
    public string Endpoint { get; set; } = "";
    public DateTime FechaHora { get; set; }
    public int CodigoHttp { get; set; }
    public long DuracionMs { get; set; }
    public string CorrelationId { get; set; } = "";
    public string? Excepcion { get; set; }
}

public class ReporteOperacionesVm
{
    public int TotalOrdenes { get; set; }
    public List<EstadoConteoOperacionVm> OrdenesPorEstado { get; set; } = [];
    public int TotalPresupuestos { get; set; }
    public int PresupuestosAprobados { get; set; }
    public decimal MontoTotalAprobado { get; set; }
    public List<RepuestoAlertaVm> RepuestosEnAlerta { get; set; } = [];
    public List<CargaMecanicoVm> CargaMecanicos { get; set; } = [];
}

public class EstadoConteoOperacionVm
{
    public string Estado { get; set; } = "";
    public int Cantidad { get; set; }
}

public class RepuestoAlertaVm
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public int Existencias { get; set; }
    public int StockMinimo { get; set; }
}

public class CargaMecanicoVm
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = "";
    public string Especialidad { get; set; } = "";
    public int AsignacionesActivas { get; set; }
}
