namespace CommunityPlatform.Domain.Entities;

public class EmployerProfileCategory
{
    public Guid EmployerProfileId { get; set; }
    public EmployerProfile EmployerProfile { get; set; } = null!;

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}