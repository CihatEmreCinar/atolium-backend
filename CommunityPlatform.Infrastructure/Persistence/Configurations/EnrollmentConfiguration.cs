using CommunityPlatform.Domain.Entities;
using CommunityPlatform.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityPlatform.Infrastructure.Persistence.Configurations;

public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("pending");

        builder.Property(e => e.AttendanceStatus)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(AttendanceStatus.Pending);

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

public class WorkshopTicketConfiguration : IEntityTypeConfiguration<WorkshopTicket>
{
    public void Configure(EntityTypeBuilder<WorkshopTicket> builder)
    {
        builder.ToTable("workshop_tickets");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Nonce).HasMaxLength(64);
        builder.Property(t => t.Signature).HasMaxLength(128);

        builder.HasOne(t => t.Enrollment)
            .WithMany(e => e.Tickets)
            .HasForeignKey(t => t.EnrollmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Bir enrollment'ın geçerli (kullanılmamış/iptal edilmemiş/süresi geçmemiş) biletini
        // hızlı bulmak için — GetTicket her açılışta bunu sorguluyor.
        builder.HasIndex(t => t.EnrollmentId)
            .HasDatabaseName("ix_workshop_tickets_enrollment_id");
    }
}
