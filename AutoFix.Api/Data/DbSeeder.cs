using AutoFix.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AutoFix.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AutoFixDbContext db)
    {
        await db.Database.MigrateAsync();

        if (!await db.Clientes.AnyAsync())
        {
            var cliente1 = new Cliente
            {
                NombreCompleto = "Carlos Mendoza",
                Dui = "04567890-1",
                Telefono = "7890-1234",
                CorreoElectronico = "cliente@autofix.com",
                Direccion = "Col. Escalón, San Salvador"
            };
            var cliente2 = new Cliente
            {
                NombreCompleto = "Ana Beatriz López",
                Dui = "01234567-8",
                Telefono = "7123-4567",
                CorreoElectronico = "ana.lopez@correo.com",
                Direccion = "Santa Tecla, La Libertad"
            };

            db.Clientes.AddRange(cliente1, cliente2);
            await db.SaveChangesAsync();

            db.Vehiculos.AddRange(
                new Vehiculo
                {
                    Placa = "P123-456",
                    Marca = "Toyota",
                    Modelo = "Corolla",
                    Anio = 2020,
                    Color = "Gris Metálico",
                    NumeroChasis = "JTDBR32E820123456",
                    ClienteId = cliente1.Id
                },
                new Vehiculo
                {
                    Placa = "P789-012",
                    Marca = "Honda",
                    Modelo = "Civic",
                    Anio = 2021,
                    Color = "Azul",
                    NumeroChasis = "2HGFC2F59MH123456",
                    ClienteId = cliente1.Id
                },
                new Vehiculo
                {
                    Placa = "P456-789",
                    Marca = "Hyundai",
                    Modelo = "Tucson",
                    Anio = 2019,
                    Color = "Blanco",
                    NumeroChasis = "KM8J33A46KU123456",
                    ClienteId = cliente2.Id
                }
            );
            await db.SaveChangesAsync();
        }

        if (!await db.Mecanicos.AnyAsync())
        {
            db.Mecanicos.AddRange(
                new Mecanico
                {
                    NombreCompleto = "Roberto Gómez",
                    Especialidad = "Motor y Transmisión",
                    Telefono = "7555-1001",
                    Email = "mecanico@autofix.com",
                    Activo = true
                },
                new Mecanico
                {
                    NombreCompleto = "Luis Martínez",
                    Especialidad = "Frenos y Suspensión",
                    Telefono = "7555-1002",
                    Email = "luis.martinez@autofix.com",
                    Activo = true
                }
            );
            await db.SaveChangesAsync();
        }

        if (!await db.Repuestos.AnyAsync())
        {
            db.Repuestos.AddRange(
                new Repuesto
                {
                    Codigo = "REP-001",
                    Nombre = "Kit Pastillas de Freno Delanteras",
                    Descripcion = "Juego de pastillas cerámicas para eje delantero",
                    Categoria = "Frenos",
                    Marca = "Brembo",
                    Precio = 45.00m,
                    Existencias = 2,
                    StockMinimo = 2
                },
                new Repuesto
                {
                    Codigo = "REP-002",
                    Nombre = "Filtro de Aceite Sintético",
                    Descripcion = "Filtro de alta eficiencia para motor 1.6-2.0L",
                    Categoria = "Motor",
                    Marca = "Bosch",
                    Precio = 12.50m,
                    Existencias = 15,
                    StockMinimo = 5
                },
                new Repuesto
                {
                    Codigo = "REP-003",
                    Nombre = "Amortiguador Delantero Gas",
                    Descripcion = "Amortiguador hidráulico presurizado",
                    Categoria = "Suspensión",
                    Marca = "Monroe",
                    Precio = 85.00m,
                    Existencias = 4,
                    StockMinimo = 4
                }
            );
            await db.SaveChangesAsync();
        }

        if (!await db.Usuarios.AnyAsync())
        {
            var hasher = new PasswordHasher<Usuario>();
            var cliente1 = await db.Clientes.OrderBy(c => c.Id).FirstOrDefaultAsync();
            var mecanico1 = await db.Mecanicos.OrderBy(m => m.Id).FirstOrDefaultAsync();

            var usuarios = new List<(Usuario User, string Password, string[] Claims)>
            {
                (
                    new Usuario { NombreCompleto = "Admin AutoFix", Email = "admin@autofix.com", Rol = RolesSistema.Administrador },
                    "Admin123!",
                    ClaimsSistema.Todos
                ),
                (
                    new Usuario { NombreCompleto = "Laura Recepción", Email = "recepcion@autofix.com", Rol = RolesSistema.Recepcionista },
                    "Recepcion123!",
                    [ClaimsSistema.OrdenesCrear, ClaimsSistema.OrdenesEditar]
                ),
                (
                    new Usuario { NombreCompleto = "Roberto Gómez (Mecánico)", Email = "mecanico@autofix.com", Rol = RolesSistema.Mecanico, MecanicoId = mecanico1?.Id },
                    "Mecanico123!",
                    [ClaimsSistema.OrdenesEditar]
                ),
                (
                    new Usuario { NombreCompleto = "Sofía Supervisor", Email = "supervisor@autofix.com", Rol = RolesSistema.Supervisor },
                    "Supervisor123!",
                    [ClaimsSistema.OrdenesAsignar, ClaimsSistema.OrdenesAprobar, ClaimsSistema.OrdenesFinalizar, ClaimsSistema.InventarioGestionar, ClaimsSistema.ReportesConsultar]
                ),
                (
                    new Usuario { NombreCompleto = "Carlos Mendoza (Cliente)", Email = "cliente@autofix.com", Rol = RolesSistema.Cliente, ClienteId = cliente1?.Id },
                    "Cliente123!",
                    []
                )
            };

            foreach (var (user, pwd, claims) in usuarios)
            {
                user.PasswordHash = hasher.HashPassword(user, pwd);
                foreach (var claim in claims)
                {
                    user.Claims.Add(new UsuarioClaim
                    {
                        ClaimType = ClaimsSistema.TipoPermiso,
                        ClaimValue = claim
                    });
                }
                db.Usuarios.Add(user);
            }

            await db.SaveChangesAsync();
        }
    }
}
