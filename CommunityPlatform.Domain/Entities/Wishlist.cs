namespace CommunityPlatform.Domain.Entities;

public class Wishlist
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid WorkshopId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
    public Workshop Workshop { get; set; } = null!;
}