using System.ComponentModel.DataAnnotations;

namespace AutoFix.Api.Models;

public class Cliente
{
    public int Id { get; set; }

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

    public ICollection<Vehiculo> Vehiculos { get; set; } = new List<Vehiculo>();
    public ICollection<OrdenReparacion> Ordenes { get; set; } = new List<OrdenReparacion>();
}

public class Vehiculo
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string Placa { get; set; } = "";

    [Required, MaxLength(100)]
    public string Marca { get; set; } = "";

    [Required, MaxLength(100)]
    public string Modelo { get; set; } = "";

    public int Anio { get; set; }

    [Required, MaxLength(50)]
    public string Color { get; set; } = "";

    [Required, MaxLength(60)]
    public string NumeroChasis { get; set; } = "";

    [MaxLength(300)]
    public string? FotografiaRuta { get; set; }

    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;

    public ICollection<OrdenReparacion> Ordenes { get; set; } = new List<OrdenReparacion>();
}
