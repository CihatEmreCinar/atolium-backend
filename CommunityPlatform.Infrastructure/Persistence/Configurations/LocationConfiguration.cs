using CommunityPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityPlatform.Infrastructure.Persistence.Configurations;

public class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.ToTable("cities");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.PlateCode).HasMaxLength(2).IsRequired();
        builder.HasIndex(c => c.PlateCode).IsUnique();
        builder.HasIndex(c => c.Name);
    }
}

public class DistrictConfiguration : IEntityTypeConfiguration<District>
{
    public void Configure(EntityTypeBuilder<District> builder)
    {
        builder.ToTable("districts");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Name).HasMaxLength(100).IsRequired();

        builder.HasOne(d => d.City)
            .WithMany(c => c.Districts)
            .HasForeignKey(d => d.CityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => new { d.CityId, d.Name });
    }
}
