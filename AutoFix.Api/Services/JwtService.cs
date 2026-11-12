using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutoFix.Api.Models;
using Microsoft.IdentityModel.Tokens;

namespace AutoFix.Api.Services;

public class JwtService
{
    private readonly IConfiguration _config;

    public JwtService(IConfiguration config)
    {
        _config = config;
    }

    public static string ObtenerClave(IConfiguration config)
    {
        var key = config["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(key))
            key = Environment.GetEnvironmentVariable("JWT_KEY");
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("No se ha configurado la clave JWT (Jwt:Key o JWT_KEY).");
        return key;
    }

    public (string Token, DateTime Expiracion) GenerarToken(Usuario usuario)
    {
        var keyBytes = Encoding.UTF8.GetBytes(ObtenerClave(_config));
        var creds = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Email),
            new(ClaimTypes.Email, usuario.Email),
            new("nombre", usuario.NombreCompleto),
            new(ClaimTypes.Role, usuario.Rol)
        };

        if (usuario.ClienteId.HasValue)
            claims.Add(new Claim("clienteId", usuario.ClienteId.Value.ToString()));

        if (usuario.MecanicoId.HasValue)
            claims.Add(new Claim("mecanicoId", usuario.MecanicoId.Value.ToString()));

        foreach (var uc in usuario.Claims)
            claims.Add(new Claim(uc.ClaimType, uc.ClaimValue));

        var expiracion = DateTime.UtcNow.AddHours(8);
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? "AutoFix.Api",
            audience: _config["Jwt:Audience"] ?? "AutoFix.Mvc",
            claims: claims,
            expires: expiracion,
            signingCredentials: creds
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiracion);
    }
}
