namespace AutoFix.Api.Models;

public enum EstadoOrden
{
    Recibido = 1,
    EnDiagnostico = 2,
    Presupuestado = 3,
    PendienteAprobacion = 4,
    Aprobado = 5,
    EnReparacion = 6,
    Finalizado = 7,
    Entregado = 8,
    Cancelado = 9
}

public static class RolesSistema
{
    public const string Administrador = "Administrador";
    public const string Recepcionista = "Recepcionista";
    public const string Mecanico = "Mecánico";
    public const string Supervisor = "Supervisor";
    public const string Cliente = "Cliente";

    public static readonly string[] Todos =
    [
        Administrador, Recepcionista, Mecanico, Supervisor, Cliente
    ];
}

public static class ClaimsSistema
{
    public const string TipoPermiso = "permiso";

    public const string OrdenesCrear = "Ordenes.Crear";
    public const string OrdenesEditar = "Ordenes.Editar";
    public const string OrdenesAsignar = "Ordenes.Asignar";
    public const string OrdenesAprobar = "Ordenes.Aprobar";
    public const string OrdenesFinalizar = "Ordenes.Finalizar";
    public const string InventarioGestionar = "Inventario.Gestionar";
    public const string ReportesConsultar = "Reportes.Consultar";

    public static readonly string[] Todos =
    [
        OrdenesCrear,
        OrdenesEditar,
        OrdenesAsignar,
        OrdenesAprobar,
        OrdenesFinalizar,
        InventarioGestionar,
        ReportesConsultar
    ];
}

public static class CategoriasEvidencia
{
    public const string VehiculoIngreso = "VehiculoIngreso";
    public const string DanioIdentificado = "DanioIdentificado";
    public const string RepuestoUtilizado = "RepuestoUtilizado";
    public const string ReparacionFinalizada = "ReparacionFinalizada";

    public static readonly string[] Todas =
    [
        VehiculoIngreso, DanioIdentificado, RepuestoUtilizado, ReparacionFinalizada
    ];
}
