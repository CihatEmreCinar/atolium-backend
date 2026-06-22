namespace CommunityPlatform.Application.DTOs.Reviews;

public class ReviewResponse
{
    public Guid Id { get; set; }
    public Guid WorkshopId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = null!;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public string? EmployerReply { get; set; }
    public DateTime CreatedAt { get; set; }
}