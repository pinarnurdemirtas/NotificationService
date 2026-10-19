using Microsoft.EntityFrameworkCore;
using NotificationService.Domain.Entities;
namespace NotificationService.Infrastructure.Persistence;
public sealed class NotificationDbContext(DbContextOptions<NotificationDbContext> options) : DbContext(options)
{
    public DbSet<Template> Templates => Set<Template>();
    public DbSet<Notification> Notifications => Set<Notification>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Template>(e => {
            e.ToTable("Templates"); e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Body).IsRequired();
        });
        modelBuilder.Entity<Notification>(e => {
            e.ToTable("Notifications"); e.HasKey(x => x.Id);
            e.Property(x => x.Recipient).HasMaxLength(4096).IsRequired();
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Body).IsRequired();
            e.Property(x => x.Channel).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.ProviderMessageId).HasMaxLength(512);
            e.HasIndex(x => new { x.Status, x.CreatedAtUtc });
            e.HasOne(x => x.Template).WithMany(x => x.Notifications)
                .HasForeignKey(x => x.TemplateId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
