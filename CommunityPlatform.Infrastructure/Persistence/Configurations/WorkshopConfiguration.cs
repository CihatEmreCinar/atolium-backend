using CommunityPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityPlatform.Infrastructure.Persistence.Configurations;

public class WorkshopConfiguration : IEntityTypeConfiguration<Workshop>
{
    public void Configure(EntityTypeBuilder<Workshop> builder)
    {
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Title).HasMaxLength(300).IsRequired();
        builder.Property(w => w.Price).HasColumnType("numeric(10,2)");
        builder.Property(w => w.AvgRating).HasColumnType("numeric(3,2)");
        builder.Property(w => w.Status).HasMaxLength(20).HasDefaultValue("draft");
        builder.Property(w => w.LocationType).HasMaxLength(20).IsRequired();
        builder.Property(w => w.Tags).HasColumnType("text[]");
        builder.Property(w => w.VenueName).HasMaxLength(255);
        builder.Property(w => w.Address).HasMaxLength(500);

        builder.HasOne(w => w.Employer)
            .WithMany()
            .HasForeignKey(w => w.EmployerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(w => w.City)
            .WithMany()
            .HasForeignKey(w => w.CityId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(w => w.District)
            .WithMany()
            .HasForeignKey(w => w.DistrictId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // Yakındaki atölye aramasında (nearby) önce City'e göre kabaca filtrelenip
        // sonra Haversine ile hassas mesafe hesaplanacak — bu index o ilk filtreyi hızlandırır.
        builder.HasIndex(w => w.CityId).HasDatabaseName("ix_workshops_city_id");

        builder.HasQueryFilter(w => w.DeletedAt == null);
    }
}