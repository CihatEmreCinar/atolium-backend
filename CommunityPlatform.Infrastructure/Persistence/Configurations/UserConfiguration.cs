using CommunityPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityPlatform.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Email).HasMaxLength(255).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.Role).HasMaxLength(30).IsRequired();
        builder.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(100).IsRequired();

        builder.HasOne(u => u.EmployerProfile)
            .WithOne(e => e.User)
            .HasForeignKey<EmployerProfile>(e => e.UserId);

        builder.HasOne(u => u.EmployeeProfile)
            .WithOne(e => e.User)
            .HasForeignKey<EmployeeProfile>(e => e.UserId);

        builder.HasOne(u => u.CafeProfile)
            .WithOne(c => c.User)
            .HasForeignKey<CafeProfile>(c => c.UserId);

        builder.HasOne(u => u.City)
            .WithMany()
            .HasForeignKey(u => u.CityId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(u => u.District)
            .WithMany()
            .HasForeignKey(u => u.DistrictId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}