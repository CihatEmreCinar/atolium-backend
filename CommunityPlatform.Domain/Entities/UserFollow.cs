namespace CommunityPlatform.Domain.Entities;
 
/// <summary>
/// Bir kullanıcının başka bir kullanıcıyı takip etmesi.
/// Employee → Employer takip eder; karşılıklı takip de mümkün.
/// Unique constraint: (FollowerId, FollowedId)
/// </summary>
public class UserFollow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FollowerId { get; set; }
    public Guid FollowedId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
 
    // Navigation
    public User Follower { get; set; } = null!;
    public User Followed { get; set; } = null!;
}