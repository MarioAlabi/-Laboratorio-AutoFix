using AutoFix.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoFix.Api.Data;

public class AutoFixDbContext : DbContext
{
    public AutoFixDbContext(DbContextOptions<AutoFixDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<UsuarioClaim> UsuarioClaims => Set<UsuarioClaim>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Vehiculo> Vehiculos => Set<Vehiculo>();
    public DbSet<Mecanico> Mecanicos => Set<Mecanico>();
    public DbSet<OrdenReparacion> OrdenesReparacion => Set<OrdenReparacion>();
    public DbSet<AsignacionMecanico> AsignacionesMecanicos => Set<AsignacionMecanico>();
    public DbSet<Presupuesto> Presupuestos => Set<Presupuesto>();
    public DbSet<PresupuestoRepuesto> PresupuestoRepuestos => Set<PresupuestoRepuesto>();
    public DbSet<Repuesto> Repuestos => Set<Repuesto>();
    public DbSet<MovimientoInventario> Movimientos => Set<MovimientoInventario>();
    public DbSet<Evidencia> Evidencias => Set<Evidencia>();
    public DbSet<LogRegistro> LogsRegistro => Set<LogRegistro>();

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        foreach (var entry in ChangeTracker.Entries<EntidadConcurrente>()
            .Where(x => x.State == EntityState.Modified))
        {
            entry.Entity.Version = Guid.NewGuid();
        }

        return base.SaveChangesAsync(ct);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Usuario>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.HasMany(u => u.Claims)
             .WithOne(c => c.Usuario)
             .HasForeignKey(c => c.UsuarioId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Cliente>(e =>
        {
            e.HasIndex(c => c.Dui).IsUnique();
            e.HasMany(c => c.Vehiculos)
             .WithOne(v => v.Cliente)
             .HasForeignKey(v => v.ClienteId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Vehiculo>(e =>
        {
            e.HasIndex(v => v.Placa).IsUnique();
        });

        modelBuilder.Entity<OrdenReparacion>(e =>
        {
            e.HasIndex(o => o.NumeroOrden).IsUnique();

            e.HasOne(o => o.Cliente)
             .WithMany(c => c.Ordenes)
             .HasForeignKey(o => o.ClienteId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(o => o.Vehiculo)
             .WithMany(v => v.Ordenes)
             .HasForeignKey(o => o.VehiculoId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(o => o.Mecanico)
             .WithMany(m => m.Ordenes)
             .HasForeignKey(o => o.MecanicoId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AsignacionMecanico>(e =>
        {
            e.HasOne(a => a.OrdenReparacion)
             .WithMany(o => o.Asignaciones)
             .HasForeignKey(a => a.OrdenReparacionId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(a => a.Mecanico)
             .WithMany(m => m.Asignaciones)
             .HasForeignKey(a => a.MecanicoId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Presupuesto>(e =>
        {
            e.Property(p => p.ManoDeObra).HasPrecision(18, 2);
            e.Property(p => p.Total).HasPrecision(18, 2);

            e.HasOne(p => p.OrdenReparacion)
             .WithOne(o => o.Presupuesto)
             .HasForeignKey<Presupuesto>(p => p.OrdenReparacionId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PresupuestoRepuesto>(e =>
        {
            e.Property(pr => pr.PrecioUnitario).HasPrecision(18, 2);
            e.Property(pr => pr.Subtotal).HasPrecision(18, 2);

            e.HasOne(pr => pr.Presupuesto)
             .WithMany(p => p.Repuestos)
             .HasForeignKey(pr => pr.PresupuestoId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(pr => pr.Repuesto)
             .WithMany()
             .HasForeignKey(pr => pr.RepuestoId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Repuesto>(e =>
        {
            e.HasIndex(r => r.Codigo).IsUnique();
            e.Property(r => r.Precio).HasPrecision(18, 2);
        });

        modelBuilder.Entity<MovimientoInventario>(e =>
        {
            e.HasOne(m => m.Repuesto)
             .WithMany(r => r.Movimientos)
             .HasForeignKey(m => m.RepuestoId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Evidencia>(e =>
        {
            e.HasOne(ev => ev.OrdenReparacion)
             .WithMany(o => o.Evidencias)
             .HasForeignKey(ev => ev.OrdenReparacionId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
