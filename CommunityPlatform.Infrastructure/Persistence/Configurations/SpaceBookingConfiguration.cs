using CommunityPlatform.Domain.Entities;
using CommunityPlatform.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityPlatform.Infrastructure.Persistence.Configurations;

public class SpaceBookingConfiguration : IEntityTypeConfiguration<SpaceBooking>
{
    public void Configure(EntityTypeBuilder<SpaceBooking> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.StartDateTime).IsRequired();
        builder.Property(b => b.EndDateTime).IsRequired();
        builder.Property(b => b.Status).HasConversion<string>().HasMaxLength(20).HasDefaultValue(SpaceBookingStatus.Pending);
        builder.Property(b => b.TotalPrice).HasColumnType("numeric(10,2)").IsRequired();
        builder.Property(b => b.Notes).HasMaxLength(1000);

        builder.HasOne(b => b.SpaceListing)
            .WithMany(l => l.SpaceBookings)
            .HasForeignKey(b => b.SpaceListingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.EmployerProfile)
            .WithMany(e => e.SpaceBookings)
            .HasForeignKey(b => b.EmployerProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
