using CommunityPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityPlatform.Infrastructure.Persistence.Configurations;

public class DeviceTokenConfiguration : IEntityTypeConfiguration<DeviceToken>
{
    public void Configure(EntityTypeBuilder<DeviceToken> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.ExpoPushToken).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Platform).IsRequired().HasMaxLength(20);

        // Bir Expo push token her zaman tek bir cihaza aittir; kullanıcı değişse
        // bile (logout/login) aynı satır güncellenir — bu yüzden global unique.
        builder.HasIndex(t => t.ExpoPushToken).IsUnique();
        builder.HasIndex(t => t.UserId);

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
