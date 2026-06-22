namespace CommunityPlatform.Domain.Entities;

public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid WorkshopId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public string? IyzicoToken { get; set; }
    public string? IyzicoPaymentId { get; set; }
    public string Status { get; set; } = "pending"; // pending | success | failed | refunded
    public DateTime? PaidAt { get; set; }
    public DateTime? RefundedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
    public Workshop Workshop { get; set; } = null!;
}