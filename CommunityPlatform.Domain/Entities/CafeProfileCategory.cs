namespace CommunityPlatform.Domain.Entities;

public class CafeProfileCategory
{
    public Guid CafeProfileId { get; set; }
    public CafeProfile CafeProfile { get; set; } = null!;

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}
