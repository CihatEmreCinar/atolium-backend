using CommunityPlatform.Domain.Entities;
using CommunityPlatform.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityPlatform.Infrastructure.Persistence.Configurations;

public class EventReminderConfiguration : IEntityTypeConfiguration<EventReminder>
{
    public void Configure(EntityTypeBuilder<EventReminder> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.SourceType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(r => r.EventStartAt).IsRequired();
        builder.Property(r => r.OffsetMinutes).IsRequired();

        // ReminderDispatchJob'un ana sorgusu (SentAt IS NULL AND ...) için indeks.
        builder.HasIndex(r => new { r.SentAt, r.EventStartAt });
        builder.HasIndex(r => new { r.SourceType, r.SourceId });

        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
