using CommunityPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityPlatform.Infrastructure.Persistence.Configurations;

public class MediaConfiguration : IEntityTypeConfiguration<Media>
{
    public void Configure(EntityTypeBuilder<Media> builder)
    {
        builder.ToTable("media");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Preset).HasConversion<string>().HasMaxLength(16);
        builder.Property(m => m.Status).HasConversion<string>().HasMaxLength(16);

        builder.Property(m => m.Bucket).HasMaxLength(128);
        builder.Property(m => m.TempObjectKey).HasMaxLength(512);
        builder.Property(m => m.ObjectKey).HasMaxLength(512);
        builder.Property(m => m.MimeType).HasMaxLength(64);
        builder.Property(m => m.Checksum).HasMaxLength(64);
        builder.Property(m => m.FailureReason).HasMaxLength(512);

        builder.HasIndex(m => m.OwnerId).HasDatabaseName("ix_media_owner");
        builder.HasIndex(m => m.Checksum).HasDatabaseName("ix_media_checksum");

        builder.HasMany(m => m.Variants)
            .WithOne(v => v.Media)
            .HasForeignKey(v => v.MediaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// MediaVariant
// ─────────────────────────────────────────────────────────────────────────────
public class MediaVariantConfiguration : IEntityTypeConfiguration<MediaVariant>
{
    public void Configure(EntityTypeBuilder<MediaVariant> builder)
    {
        builder.ToTable("media_variant");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.ObjectKey).HasMaxLength(512);

        builder.HasIndex(v => new { v.MediaId, v.Width })
            .IsUnique()
            .HasDatabaseName("ix_media_variant_media_width");
    }
}