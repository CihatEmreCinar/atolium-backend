namespace CommunityPlatform.Domain.Entities;

public class EmployeeProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public List<string> Interests { get; set; } = [];
    public List<string> Hobbies { get; set; } = [];
    public string? CoverImageUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
}