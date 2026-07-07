namespace CommunityPlatform.Domain.Entities;

public class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? IconUrl { get; set; }
    public Guid? ParentId { get; set; }
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    // Navigation
    public Category? Parent { get; set; }
    public ICollection<Category> Children { get; set; } = [];
    public ICollection<WorkshopCategory> WorkshopCategories { get; set; } = [];
    public ICollection<EmployerProfileCategory> EmployerProfileCategories { get; set; } = [];
    public ICollection<CafeProfileCategory> CafeProfileCategories { get; set; } = [];
}