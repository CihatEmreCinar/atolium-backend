using CommunityPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityPlatform.Infrastructure.Persistence.Configurations;

public class EmployeeProfileConfiguration : IEntityTypeConfiguration<EmployeeProfile>
{
    public void Configure(EntityTypeBuilder<EmployeeProfile> builder)
    {
        builder.HasOne(e => e.PreferredCity)
            .WithMany()
            .HasForeignKey(e => e.PreferredCityId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.PreferredDistrict)
            .WithMany()
            .HasForeignKey(e => e.PreferredDistrictId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
