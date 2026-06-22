using CommunityPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityPlatform.Infrastructure.Persistence.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Rating).IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_Review_Rating", "\"Rating\" >= 1 AND \"Rating\" <= 5"));
        builder.HasIndex(r => new { r.WorkshopId, r.UserId }).IsUnique();

        builder.HasOne(r => r.Workshop)
            .WithMany(w => w.Reviews)
            .HasForeignKey(r => r.WorkshopId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.User)
            .WithMany(u => u.Reviews)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}