using LicitApp.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace LicitApp.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Solicitud> Solicitudes => Set<Solicitud>();
    public DbSet<Material> Materiales => Set<Material>();
    public DbSet<Oferta> Ofertas => Set<Oferta>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // Enums como texto: legibles en la DB y alineados con lo que espera el front.
        b.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(u => u.Uid);
            e.Property(u => u.Role).HasConversion<string>().HasMaxLength(32);
            e.HasIndex(u => u.Role);
            e.HasIndex(u => u.Zone);

            // Stats como owned -> columnas planas stats_total_*.
            e.OwnsOne(u => u.Stats, s =>
            {
                s.Property(x => x.TotalLicitaciones).HasColumnName("stats_total_licitaciones");
                s.Property(x => x.TotalOfertas).HasColumnName("stats_total_ofertas");
                s.Property(x => x.TotalCierres).HasColumnName("stats_total_cierres");
            });
        });

        b.Entity<Solicitud>(e =>
        {
            e.ToTable("solicitudes");
            e.HasKey(s => s.Id);
            e.Property(s => s.Status).HasConversion<string>().HasMaxLength(32);
            e.HasIndex(s => s.ConstructorId);
            e.HasIndex(s => new { s.DeliveryZone, s.Status });
            e.HasIndex(s => s.CreatedAt);

            e.HasOne<User>()
                .WithMany()
                .HasForeignKey(s => s.ConstructorId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(s => s.Materiales)
                .WithOne(m => m.Solicitud!)
                .HasForeignKey(m => m.SolicitudId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(s => s.Ofertas)
                .WithOne(o => o.Solicitud!)
                .HasForeignKey(o => o.SolicitudId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Material>(e =>
        {
            e.ToTable("materiales");
            e.HasKey(m => m.Id);
            e.Property(m => m.Quantity).HasColumnType("numeric(18,3)");
        });

        b.Entity<Oferta>(e =>
        {
            e.ToTable("ofertas");
            e.HasKey(o => o.Id);
            e.Property(o => o.Status).HasConversion<string>().HasMaxLength(32);
            e.Property(o => o.ShippingType).HasConversion<string>().HasMaxLength(32);
            e.Property(o => o.TotalPrice).HasColumnType("numeric(18,2)");
            e.Property(o => o.ShippingPrice).HasColumnType("numeric(18,2)");
            e.Property(o => o.AttachmentUrl).HasMaxLength(2048);
            e.HasIndex(o => o.SolicitudId);
            e.HasIndex(o => o.CorralonId);
            e.HasIndex(o => new { o.SolicitudId, o.Status });

            e.HasOne<User>()
                .WithMany()
                .HasForeignKey(o => o.CorralonId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Notification>(e =>
        {
            e.ToTable("notifications");
            e.HasKey(n => n.Id);
            e.Property(n => n.Type).HasConversion<string>().HasMaxLength(32);
            e.HasIndex(n => new { n.UserId, n.Read });

            e.HasOne<User>()
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
