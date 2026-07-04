namespace CommunityPlatform.Application.DTOs.Cafe;

public class CafeProfileResponse
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = null!;
    public string? Bio { get; set; }
    public string? City { get; set; }
    public string? Address { get; set; }
    public string? AvatarUrl { get; set; }
    public string? CoverImageUrl { get; set; }
}
