namespace CommunityPlatform.Application.DTOs.Admin;

public class UserListItem
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Role { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UpdateUserStatusRequest
{
    public bool IsActive { get; set; }
}

public class WorkshopModerationItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string EmployerName { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

public class RejectWorkshopRequest
{
    public string Reason { get; set; } = null!;
}