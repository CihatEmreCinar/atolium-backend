namespace CommunityPlatform.Application.DTOs.Wishlist;

public class WishlistResponse
{
    public Guid Id { get; set; }
    public Guid WorkshopId { get; set; }
    public string WorkshopTitle { get; set; } = null!;
    public string EmployerName { get; set; } = null!;
    public decimal Price { get; set; }
    public DateTime StartAt { get; set; }
    public string Status { get; set; } = null!;
    public decimal AvgRating { get; set; }
    public DateTime CreatedAt { get; set; }
}