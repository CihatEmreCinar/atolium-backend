namespace CommunityPlatform.Application.DTOs.Social;

// ─── Like ────────────────────────────────────────────────────────────────────

public class LikeResponse
{
    public bool IsLiked { get; set; }
    public int LikeCount { get; set; }
}

// ─── Comment ─────────────────────────────────────────────────────────────────

public class CreateCommentRequest
{
    public string Content { get; set; } = null!;
    public Guid? ParentCommentId { get; set; }
}

public class CommentResponse
{
    public Guid Id { get; set; }
    public Guid PostId { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = null!;
    public string? AuthorAvatarUrl { get; set; }
    public Guid? ParentCommentId { get; set; }
    public string Content { get; set; } = null!;
    public int LikeCount { get; set; }
    public List<CommentResponse> Replies { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class CommentListResponse
{
    public List<CommentResponse> Comments { get; set; } = new();
    public string? NextCursor { get; set; }
    public bool HasNextPage { get; set; }
}

// ─── Follow ──────────────────────────────────────────────────────────────────

public class FollowResponse
{
    public bool IsFollowing { get; set; }
    public int FollowerCount { get; set; }
}

public class FollowListResponse
{
    public List<FollowUserResponse> Users { get; set; } = new();
    public string? NextCursor { get; set; }
    public bool HasNextPage { get; set; }
}

public class FollowUserResponse
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public bool IsFollowingBack { get; set; }
}

// ─── Share ───────────────────────────────────────────────────────────────────

public class ShareResponse
{
    public Guid ShareToken { get; set; }
    public string ShareUrl { get; set; } = null!;
}