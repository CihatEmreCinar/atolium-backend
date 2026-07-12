using CommunityPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CommunityPlatform.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // ── Mevcut ──────────────────────────────────────────────────────────────
    public DbSet<User> Users => Set<User>();
    public DbSet<EmployerProfile> EmployerProfiles => Set<EmployerProfile>();
    public DbSet<EmployeeProfile> EmployeeProfiles => Set<EmployeeProfile>();
    public DbSet<CafeProfile> CafeProfiles => Set<CafeProfile>();
    public DbSet<SpaceListing> SpaceListings => Set<SpaceListing>();
    public DbSet<SpaceListingPhoto> SpaceListingPhotos => Set<SpaceListingPhoto>();
    public DbSet<SpaceAvailability> SpaceAvailabilities => Set<SpaceAvailability>();
    public DbSet<SpaceBooking> SpaceBookings => Set<SpaceBooking>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Workshop> Workshops => Set<Workshop>();
    public DbSet<WorkshopCategory> WorkshopCategories => Set<WorkshopCategory>();
    public DbSet<EmployerProfileCategory> EmployerProfileCategories => Set<EmployerProfileCategory>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Badge> Badges => Set<Badge>();
    public DbSet<UserBadge> UserBadges => Set<UserBadge>();
    public DbSet<Wishlist> Wishlists => Set<Wishlist>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    // ── Reminder / Push ─────────────────────────────────────────────────────
    public DbSet<EventReminder> EventReminders => Set<EventReminder>();
    public DbSet<DeviceToken> DeviceTokens => Set<DeviceToken>();

    // ── Sosyal Feed ─────────────────────────────────────────────────────────
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<PostMedia> PostMedia => Set<PostMedia>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<PostTag> PostTags => Set<PostTag>();
    public DbSet<PostLike> PostLikes => Set<PostLike>();
    public DbSet<PostComment> PostComments => Set<PostComment>();
    public DbSet<PostShare> PostShares => Set<PostShare>();
    public DbSet<UserFollow> UserFollows => Set<UserFollow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // SocialEntityConfigurations.cs dahil tüm IEntityTypeConfiguration<T>'lar otomatik yüklenir.
        // UserFollow konfigürasyonu SocialEntityConfigurations.cs'de tanımlı — buraya ekleme yapma.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // ── WorkshopCategory ────────────────────────────────────────────────
        modelBuilder.Entity<WorkshopCategory>()
            .HasKey(wc => new { wc.WorkshopId, wc.CategoryId });

        modelBuilder.Entity<WorkshopCategory>()
            .HasOne(wc => wc.Workshop)
            .WithMany(w => w.WorkshopCategories)
            .HasForeignKey(wc => wc.WorkshopId);

        modelBuilder.Entity<WorkshopCategory>()
            .HasOne(wc => wc.Category)
            .WithMany(c => c.WorkshopCategories)
            .HasForeignKey(wc => wc.CategoryId);

        // ── EmployerProfileCategory ─────────────────────────────────────────
        modelBuilder.Entity<EmployerProfileCategory>()
            .HasKey(ec => new { ec.EmployerProfileId, ec.CategoryId });

        modelBuilder.Entity<EmployerProfileCategory>()
            .HasOne(ec => ec.EmployerProfile)
            .WithMany(p => p.EmployerProfileCategories)
            .HasForeignKey(ec => ec.EmployerProfileId);

        modelBuilder.Entity<EmployerProfileCategory>()
            .HasOne(ec => ec.Category)
            .WithMany(c => c.EmployerProfileCategories)
            .HasForeignKey(ec => ec.CategoryId);

        // ── CafeProfileCategory (mirror of EmployerProfileCategory)
        modelBuilder.Entity<CafeProfileCategory>()
            .HasKey(cc => new { cc.CafeProfileId, cc.CategoryId });

        modelBuilder.Entity<CafeProfileCategory>()
            .HasOne(cc => cc.CafeProfile)
            .WithMany(p => p.CafeProfileCategories)
            .HasForeignKey(cc => cc.CafeProfileId);

        modelBuilder.Entity<CafeProfileCategory>()
            .HasOne(cc => cc.Category)
            .WithMany(c => c.CafeProfileCategories)
            .HasForeignKey(cc => cc.CategoryId);

        // ── Seed ────────────────────────────────────────────────────────────
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Seramik",       Slug = "seramik",       SortOrder = 1 },
            new Category { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Resim",         Slug = "resim",         SortOrder = 2 },
            new Category { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Fotoğrafçılık", Slug = "fotografcilik", SortOrder = 3 },
            new Category { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), Name = "Yazılım",       Slug = "yazilim",       SortOrder = 4 },
            new Category { Id = Guid.Parse("55555555-5555-5555-5555-555555555555"), Name = "Müzik",         Slug = "muzik",         SortOrder = 5 },
            new Category { Id = Guid.Parse("66666666-6666-6666-6666-666666666666"), Name = "Dans",          Slug = "dans",          SortOrder = 6 },
            new Category { Id = Guid.Parse("77777777-7777-7777-7777-777777777777"), Name = "Yemek",         Slug = "yemek",         SortOrder = 7 },
            new Category { Id = Guid.Parse("88888888-8888-8888-8888-888888888888"), Name = "El Sanatları",  Slug = "el-sanatlari",  SortOrder = 8 }
        );

        base.OnModelCreating(modelBuilder);
    }
}