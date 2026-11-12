using System.ComponentModel.DataAnnotations;

namespace AutoFix.Api.DTOs;

public class RegisterRequest
{
    [Required, MaxLength(150)]
    public string NombreCompleto { get; set; } = "";

    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; set; } = "";

    [Required, MinLength(6)]
    public string Password { get; set; } = "";

    public string? Rol { get; set; }
}

public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";
}

public class AuthResponse
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
