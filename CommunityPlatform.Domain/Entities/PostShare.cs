namespace CommunityPlatform.Domain.Entities;

public class PostShare
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PostId { get; set; }
    public Guid SharedById { get; set; }

    // Public share linki için token — revoke veya analitik gerekirse bu kayıt silinir
    public Guid ShareToken { get; set; } = Guid.NewGuid();

    public int ViewCount { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Post Post { get; set; } = null!;
    public User SharedBy { get; set; } = null!;
}
