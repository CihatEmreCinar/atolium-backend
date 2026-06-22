using CommunityPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityPlatform.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Amount).HasColumnType("numeric(10,2)").IsRequired();
        builder.Property(p => p.Currency).HasMaxLength(10).HasDefaultValue("TRY");
        builder.Property(p => p.Status).HasMaxLength(20).HasDefaultValue("pending");

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Workshop)
            .WithMany()
            .HasForeignKey(p => p.WorkshopId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}