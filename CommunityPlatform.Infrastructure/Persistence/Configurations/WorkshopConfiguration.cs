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

        builder.HasOne(w => w.Employer)
            .WithMany()
            .HasForeignKey(w => w.EmployerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(w => w.DeletedAt == null);
       // M-2: Postgres'in her tabloda var olan xmin sistem kolonunu concurrency
       // token olarak kullanıyoruz — kapasite onaylamadaki race condition'ı önler.
        builder.Property(w => w.Version)
           .HasColumnName("xmin")
           .HasColumnType("xid")
           .ValueGeneratedOnAddOrUpdate()
           .IsConcurrencyToken();

    }
}