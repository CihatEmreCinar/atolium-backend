using CommunityPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityPlatform.Infrastructure.Persistence.Configurations;

public class WishlistConfiguration : IEntityTypeConfiguration<Wishlist>
{
    public void Configure(EntityTypeBuilder<Wishlist> builder)
    {
        builder.HasKey(w => w.Id);
        builder.HasIndex(w => new { w.UserId, w.WorkshopId }).IsUnique();

        builder.HasOne(w => w.User)
            .WithMany(u => u.Wishlists)
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(w => w.Workshop)
            .WithMany()
            .HasForeignKey(w => w.WorkshopId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}