using System.ComponentModel.DataAnnotations;

namespace AutoFix.Api.Models;

public class Usuario
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string NombreCompleto { get; set; } = "";

    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; set; } = "";

    [Required]
    public string PasswordHash { get; set; } = "";

    [Required, MaxLength(50)]
    public string Rol { get; set; } = RolesSistema.Cliente;

    public int? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public int? MecanicoId { get; set; }
    public Mecanico? Mecanico { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public ICollection<UsuarioClaim> Claims { get; set; } = new List<UsuarioClaim>();
}

public class UsuarioClaim
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    [Required, MaxLength(50)]
    public string ClaimType { get; set; } = ClaimsSistema.TipoPermiso;

    [Required, MaxLength(100)]
    public string ClaimValue { get; set; } = "";
}
