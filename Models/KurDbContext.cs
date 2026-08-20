using Microsoft.EntityFrameworkCore;

namespace KurTakipApi.Models;

public class KurDbContext : DbContext
{
    public KurDbContext(DbContextOptions<KurDbContext> options) : base(options)
    {
    }

    public DbSet<KurKayit> KurKayitlari { get; set; } = null!;
    public DbSet<KurAlarm> KurAlarmlari { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<KurKayit>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Sembol).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Kategori).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Fiyat).HasColumnType("TEXT"); // SQLite decimal desteği için TEXT/NUMERIC
            entity.Property(e => e.Kaynak).HasMaxLength(50);
        });

        modelBuilder.Entity<KurAlarm>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Sembol).IsRequired().HasMaxLength(20);
            entity.Property(e => e.EsikDeger).HasColumnType("TEXT");
            entity.Property(e => e.Aciklama).HasMaxLength(200);
        });
    }
}
