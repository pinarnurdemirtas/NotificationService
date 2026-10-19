using Microsoft.EntityFrameworkCore;
using NotificationService.Domain.Entities;
namespace NotificationService.Infrastructure.Persistence;
public sealed class NotificationDbContext(DbContextOptions<NotificationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserDevice> UserDevices => Set<UserDevice>();
    public DbSet<Template> Templates => Set<Template>();
    public DbSet<Notification> Notifications => Set<Notification>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationDbContext).Assembly);
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
            e.Property(x => x.TargetKind).HasConversion<string>().HasMaxLength(30).HasDefaultValue(PushTargetKind.FcmToken);
            e.Property(x => x.DataJson).HasColumnType("jsonb");
            e.Property(x => x.ErrorCode).HasMaxLength(100);

            e.Property(x => x.ProviderMessageId).HasMaxLength(512);
            e.HasIndex(x => new { x.Status, x.CreatedAtUtc });
            e.HasIndex(x => new { x.SenderUserId, x.CreatedAtUtc });
            e.HasIndex(x => new { x.RecipientUserId, x.CreatedAtUtc });
            e.HasIndex(x => x.BatchId);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.SenderUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.RecipientUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<UserDevice>().WithMany().HasForeignKey(x => x.UserDeviceId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Template).WithMany(x => x.Notifications)
                .HasForeignKey(x => x.TemplateId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
