namespace CommunityPlatform.Domain.Entities;

public class WorkshopTicket
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EnrollmentId { get; set; }

    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public bool Revoked { get; set; } = false;

    // İmza payload'ının parçası — her yeni bilet için farklı, tahmin edilemez (128-bit rastgele).
    public string Nonce { get; set; } = null!;

    // HMAC-SHA256("{TicketId:N}.{Nonce}.{ExpiresAtUnix}") — QR'daki imzayla karşılaştırılır.
    public string Signature { get; set; } = null!;

    // Navigation
    public Enrollment Enrollment { get; set; } = null!;
}
