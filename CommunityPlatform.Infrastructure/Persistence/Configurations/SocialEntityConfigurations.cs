using CommunityPlatform.Domain.Entities;
using CommunityPlatform.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityPlatform.Infrastructure.Persistence.Configurations;

// ─────────────────────────────────────────────────────────────────────────────
// Post
// ─────────────────────────────────────────────────────────────────────────────
public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.ToTable("posts");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Caption).HasColumnType("text");
        builder.Property(p => p.EngagementScore).HasDefaultValue(0.0);

        builder.Property(p => p.AuthorType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(PostAuthorType.Employer);

        builder.Property(p => p.Visibility)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(PostVisibility.Public);

        // Employer post'u: EmployerId dolu + CafeId null. Cafe post'u: CafeId dolu + EmployerId null.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Post_AuthorConsistency",
            "(\"AuthorType\" = 'Employer' AND \"EmployerId\" IS NOT NULL AND \"CafeId\" IS NULL) " +
            "OR (\"AuthorType\" = 'Cafe' AND \"CafeId\" IS NOT NULL AND \"EmployerId\" IS NULL)"
        ));

        builder.HasOne(p => p.Employer)
            .WithMany()
            .HasForeignKey(p => p.EmployerId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Workshop)
            .WithMany()
            .HasForeignKey(p => p.WorkshopId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Cafe)
            .WithMany()
            .HasForeignKey(p => p.CafeId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.EmployerId, p.EngagementScore, p.Id })
            .HasDatabaseName("ix_posts_feed_cursor");

        builder.HasIndex(p => p.PublishedAt)
            .IsDescending()
            .HasDatabaseName("ix_posts_published_at");

        builder.HasIndex(p => p.WorkshopId)
            .HasDatabaseName("ix_posts_workshop_id");

        builder.HasIndex(p => p.CafeId)
            .HasDatabaseName("ix_posts_cafe_id");

        builder.HasIndex(p => new { p.EngagementScore, p.Id })
            .IsDescending()
            .HasDatabaseName("ix_posts_engagement_score");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// PostMedia
// ─────────────────────────────────────────────────────────────────────────────
public class PostMediaConfiguration : IEntityTypeConfiguration<PostMedia>
{
    public void Configure(EntityTypeBuilder<PostMedia> builder)
    {
        builder.ToTable("post_media");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.MediaType)
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(m => m.StorageKey).HasMaxLength(512);
        builder.Property(m => m.CdnUrl).HasMaxLength(512);

        builder.HasOne(m => m.Post)
            .WithMany(p => p.Media)
            .HasForeignKey(m => m.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => new { m.PostId, m.OrderIndex })
            .HasDatabaseName("ix_post_media_post_order");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Tag
// ─────────────────────────────────────────────────────────────────────────────
public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("tags");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name).HasMaxLength(64).IsRequired();
        builder.Property(t => t.Slug).HasMaxLength(64).IsRequired();

        builder.HasIndex(t => t.Slug)
            .IsUnique()
            .HasDatabaseName("uq_tags_slug");

        builder.HasIndex(t => t.UsageCount)
            .IsDescending()
            .HasDatabaseName("ix_tags_usage_count");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// PostTag
// ─────────────────────────────────────────────────────────────────────────────
public class PostTagConfiguration : IEntityTypeConfiguration<PostTag>
{
    public void Configure(EntityTypeBuilder<PostTag> builder)
    {
        builder.ToTable("post_tags");
        builder.HasKey(pt => new { pt.PostId, pt.TagId });

        builder.HasOne(pt => pt.Post)
            .WithMany(p => p.PostTags)
            .HasForeignKey(pt => pt.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pt => pt.Tag)
            .WithMany(t => t.PostTags)
            .HasForeignKey(pt => pt.TagId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(pt => pt.TagId)
            .HasDatabaseName("ix_post_tags_tag_id");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// PostLike
// ─────────────────────────────────────────────────────────────────────────────
public class PostLikeConfiguration : IEntityTypeConfiguration<PostLike>
{
    public void Configure(EntityTypeBuilder<PostLike> builder)
    {
        builder.ToTable("post_likes");
        builder.HasKey(l => l.Id);

        builder.HasOne(l => l.Post)
            .WithMany(p => p.Likes)
            .HasForeignKey(l => l.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.User)
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(l => new { l.PostId, l.UserId })
            .IsUnique()
            .HasDatabaseName("uq_post_likes_post_user");

        builder.HasIndex(l => l.UserId)
            .HasDatabaseName("ix_post_likes_user_id");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// PostComment
// FIX: ParentComment cascade → SetNull
// Parent yorum silinince reply'lar silinmez, ParentCommentId NULL olur.
// Böylece post → comment cascade + comment → comment cascade çakışması önlenir.
// ─────────────────────────────────────────────────────────────────────────────
public class PostCommentConfiguration : IEntityTypeConfiguration<PostComment>
{
    public void Configure(EntityTypeBuilder<PostComment> builder)
    {
        builder.ToTable("post_comments");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Content)
            .HasColumnType("text")
            .IsRequired();

        builder.HasOne(c => c.Post)
            .WithMany(p => p.Comments)
            .HasForeignKey(c => c.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Author)
            .WithMany()
            .HasForeignKey(c => c.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);

        // FIX: Cascade → SetNull — çoklu cascade path hatasını önler
        builder.HasOne(c => c.ParentComment)
            .WithMany(c => c.Replies)
            .HasForeignKey(c => c.ParentCommentId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasIndex(c => new { c.PostId, c.ParentCommentId })
            .HasDatabaseName("ix_post_comments_post_parent");

        builder.HasIndex(c => c.AuthorId)
            .HasDatabaseName("ix_post_comments_author");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// PostShare
// ─────────────────────────────────────────────────────────────────────────────
public class PostShareConfiguration : IEntityTypeConfiguration<PostShare>
{
    public void Configure(EntityTypeBuilder<PostShare> builder)
    {
        builder.ToTable("post_shares");
        builder.HasKey(s => s.Id);

        builder.HasOne(s => s.Post)
            .WithMany(p => p.Shares)
            .HasForeignKey(s => s.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.SharedBy)
            .WithMany()
            .HasForeignKey(s => s.SharedById)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.ShareToken)
            .IsUnique()
            .HasDatabaseName("uq_post_shares_token");

        builder.HasIndex(s => s.PostId)
            .HasDatabaseName("ix_post_shares_post_id");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// UserFollow
// FIX: WithMany() — User entity'sinde Followers/Following navigation yok.
// AppDbContext'teki WithMany(u => u.Followers) kaldırıldı, sadece burada tanımlı.
// ─────────────────────────────────────────────────────────────────────────────
public class UserFollowConfiguration : IEntityTypeConfiguration<UserFollow>
{
    public void Configure(EntityTypeBuilder<UserFollow> builder)
    {
        builder.ToTable("user_follows");
        builder.HasKey(f => f.Id);

        // WithMany() boş değil — User'daki navigation'lara bağla
        builder.HasOne(f => f.Follower)
            .WithMany(u => u.Following)          // ← User.Following
            .HasForeignKey(f => f.FollowerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.Followed)
            .WithMany(u => u.Followers)          // ← User.Followers
            .HasForeignKey(f => f.FollowedId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(f => new { f.FollowerId, f.FollowedId })
            .IsUnique()
            .HasDatabaseName("uq_user_follows_pair");

        builder.HasIndex(f => f.FollowedId)
            .HasDatabaseName("ix_user_follows_followed_id");

        builder.HasIndex(f => f.FollowerId)
            .HasDatabaseName("ix_user_follows_follower_id");
    }
}