using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationService.Domain.Entities;
namespace NotificationService.Infrastructure.Persistence.Configurations;
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> e)
    {
        e.ToTable("Users"); e.HasKey(x => x.Id);
        e.Property(x => x.Username).HasMaxLength(50).IsRequired();
        e.Property(x => x.NormalizedUsername).HasMaxLength(50).IsRequired();
        e.HasIndex(x => x.NormalizedUsername).IsUnique();
        e.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
        e.Property(x => x.PasswordHash).HasMaxLength(1000).IsRequired();
    }
}
