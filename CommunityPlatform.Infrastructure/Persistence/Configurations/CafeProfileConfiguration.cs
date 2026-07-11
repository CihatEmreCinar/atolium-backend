using CommunityPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityPlatform.Infrastructure.Persistence.Configurations;

public class CafeProfileConfiguration : IEntityTypeConfiguration<CafeProfile>
{
    public void Configure(EntityTypeBuilder<CafeProfile> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(255).IsRequired();
        builder.Property(c => c.Bio).HasMaxLength(1000);
        builder.Property(c => c.City).HasMaxLength(120);
        builder.Property(c => c.Address).HasMaxLength(500);
        builder.Property(c => c.AvatarUrl).HasMaxLength(500);
        builder.Property(c => c.CoverImageUrl).HasMaxLength(500);
        builder.Property(c => c.AvgRating).HasColumnType("numeric(3,2)");

        builder.HasOne(c => c.User)
            .WithOne(u => u.CafeProfile)
            .HasForeignKey<CafeProfile>(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
