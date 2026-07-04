using CommunityPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityPlatform.Infrastructure.Persistence.Configurations;

public class SpaceAvailabilityConfiguration : IEntityTypeConfiguration<SpaceAvailability>
{
    public void Configure(EntityTypeBuilder<SpaceAvailability> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.DayOfWeek).IsRequired();
        builder.Property(s => s.StartTime).IsRequired();
        builder.Property(s => s.EndTime).IsRequired();

        builder.HasOne(s => s.SpaceListing)
            .WithMany(l => l.SpaceAvailabilities)
            .HasForeignKey(s => s.SpaceListingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
