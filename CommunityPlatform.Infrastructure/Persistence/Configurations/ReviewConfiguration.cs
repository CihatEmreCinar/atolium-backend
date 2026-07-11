using CommunityPlatform.Domain.Entities;
using CommunityPlatform.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityPlatform.Infrastructure.Persistence.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Rating).IsRequired();
        builder.Property(r => r.RevieweeType).HasConversion<string>().HasMaxLength(20).HasDefaultValue(RevieweeType.Employer);

        builder.HasIndex(r => new { r.WorkshopId, r.UserId })
            .IsUnique()
            .HasFilter("\"WorkshopId\" IS NOT NULL");

        builder.HasIndex(r => new { r.SpaceBookingId, r.UserId })
            .IsUnique()
            .HasFilter("\"SpaceBookingId\" IS NOT NULL");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Review_Rating", "\"Rating\" >= 1 AND \"Rating\" <= 5");
            t.HasCheckConstraint(
                "CK_Review_RevieweeConsistency",
                "(\"RevieweeType\" = 'Employer' AND \"WorkshopId\" IS NOT NULL AND \"SpaceBookingId\" IS NULL) " +
                "OR (\"RevieweeType\" = 'Cafe' AND \"SpaceBookingId\" IS NOT NULL AND \"WorkshopId\" IS NULL)");
        });

        builder.HasOne(r => r.Workshop)
            .WithMany(w => w.Reviews)
            .HasForeignKey(r => r.WorkshopId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.User)
            .WithMany(u => u.Reviews)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.SpaceBooking)
            .WithMany()
            .HasForeignKey(r => r.SpaceBookingId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}