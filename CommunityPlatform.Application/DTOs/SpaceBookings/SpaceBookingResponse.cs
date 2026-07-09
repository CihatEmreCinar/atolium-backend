namespace CommunityPlatform.Application.DTOs.SpaceBookings;

public class SpaceBookingResponse
{
    public Guid Id { get; set; }
    public Guid SpaceListingId { get; set; }
    public Guid EmployerProfileId { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public string Status { get; set; } = null!;
    public decimal TotalPrice { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // SpaceListing / CafeProfile embed
    public string? SpaceListingTitle { get; set; }
    public Guid? CafeProfileId { get; set; }
    public string? CafeName { get; set; }
    public string? CafeCity { get; set; }

    // EmployerProfile embed
    public string? EmployerWorkshopTitle { get; set; }
    public string? EmployerFullName { get; set; }
}
