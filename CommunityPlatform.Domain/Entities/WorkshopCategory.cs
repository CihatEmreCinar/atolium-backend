namespace CommunityPlatform.Domain.Entities;

public class WorkshopCategory
{
    public Guid WorkshopId { get; set; }
    public Workshop Workshop { get; set; } = null!;

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}