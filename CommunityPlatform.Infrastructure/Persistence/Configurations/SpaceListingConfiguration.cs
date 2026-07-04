using CommunityPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityPlatform.Infrastructure.Persistence.Configurations;

public class SpaceListingConfiguration : IEntityTypeConfiguration<SpaceListing>
{
    public void Configure(EntityTypeBuilder<SpaceListing> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Title).HasMaxLength(300).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(2000);
        builder.Property(s => s.Capacity).IsRequired();
        builder.Property(s => s.HourlyPrice).HasColumnType("numeric(10,2)").IsRequired();
        builder.Property(s => s.Amenities).HasColumnType("text[]" );
        builder.Property(s => s.IsActive).HasDefaultValue(true);

        builder.HasOne(s => s.CafeProfile)
            .WithMany(c => c.SpaceListings)
            .HasForeignKey(s => s.CafeProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
