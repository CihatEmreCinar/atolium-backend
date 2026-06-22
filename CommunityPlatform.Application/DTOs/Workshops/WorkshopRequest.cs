namespace CommunityPlatform.Application.DTOs.Workshops;

public class WorkshopRequest
{
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public decimal Price { get; set; }
    public int Capacity { get; set; }
    public string LocationType { get; set; } = null!; // online | in-person
    public string? LocationDetail { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public List<Guid>? CategoryIds { get; set; }
    public List<string>? Tags { get; set; }
}