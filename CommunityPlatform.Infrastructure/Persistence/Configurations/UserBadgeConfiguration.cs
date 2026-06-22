using CommunityPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityPlatform.Infrastructure.Persistence.Configurations;

public class UserBadgeConfiguration : IEntityTypeConfiguration<UserBadge>
{
    public void Configure(EntityTypeBuilder<UserBadge> builder)
    {
        builder.HasKey(ub => ub.Id);
        builder.HasIndex(ub => new { ub.UserId, ub.BadgeId }).IsUnique();

        builder.HasOne(ub => ub.User)
            .WithMany(u => u.UserBadges)
            .HasForeignKey(ub => ub.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ub => ub.Badge)
            .WithMany(b => b.UserBadges)
            .HasForeignKey(ub => ub.BadgeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}