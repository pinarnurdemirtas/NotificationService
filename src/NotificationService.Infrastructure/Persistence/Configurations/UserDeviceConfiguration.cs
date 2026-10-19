using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationService.Domain.Entities;
namespace NotificationService.Infrastructure.Persistence.Configurations;
public sealed class UserDeviceConfiguration : IEntityTypeConfiguration<UserDevice>
{
    public void Configure(EntityTypeBuilder<UserDevice> e)
    {
        e.ToTable("UserDevices"); e.HasKey(x => x.Id);
        e.Property(x => x.Token).HasMaxLength(4096).IsRequired();
        e.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
        e.HasIndex(x => x.TokenHash).IsUnique().HasFilter("\"IsActive\" = TRUE");
        e.Property(x => x.Name).HasMaxLength(100).IsRequired();
        e.Property(x => x.TargetKind).HasConversion<string>().HasMaxLength(30);
        e.HasIndex(x => new { x.UserId, x.IsActive });
        e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
