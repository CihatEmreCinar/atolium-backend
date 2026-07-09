namespace CommunityPlatform.Application.DTOs.SpaceBookings;

public class CreateSpaceBookingRequest
{
    public Guid SpaceListingId { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public string? Notes { get; set; }
}
