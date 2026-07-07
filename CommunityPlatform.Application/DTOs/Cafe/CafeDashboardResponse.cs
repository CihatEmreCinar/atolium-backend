namespace CommunityPlatform.Application.DTOs.Cafe;

public class CafeDashboardResponse
{
    public string Name { get; set; } = null!;
    public int TotalListings { get; set; }
    public int ActiveListings { get; set; }
    public int CategoryCount { get; set; }
}
