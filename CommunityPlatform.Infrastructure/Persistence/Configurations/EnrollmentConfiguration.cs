using CommunityPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityPlatform.Infrastructure.Persistence.Configurations;

public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("pending");
        builder.Property(e => e.TicketCode).HasMaxLength(64).IsRequired();
        builder.HasIndex(e => e.TicketCode).IsUnique();
        builder.HasIndex(e => new { e.WorkshopId, e.UserId }).IsUnique();

        builder.HasOne(e => e.Workshop)
            .WithMany(w => w.Enrollments)
            .HasForeignKey(e => e.WorkshopId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.User)
            .WithMany(u => u.Enrollments)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}