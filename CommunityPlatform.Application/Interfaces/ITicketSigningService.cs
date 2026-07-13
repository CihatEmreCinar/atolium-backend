namespace CommunityPlatform.Application.Interfaces;

/// <summary>
/// QR bilet imzalama/doğrulama. QR içeriği asla WorkshopId/UserId/EnrollmentId gibi
/// plain veri taşımaz — sadece TicketId + bu servisin ürettiği imza taşır.
/// </summary>
public interface ITicketSigningService
{
    /// <summary>128-bit kriptografik rastgele nonce üretir (her yeni bilet için bir kez).</summary>
    string GenerateNonce();

    /// <summary>HMAC-SHA256 imza üretir — TicketId, Nonce ve ExpiresAt'e bağlıdır.</summary>
    string Sign(Guid ticketId, string nonce, DateTime expiresAtUtc);

    /// <summary>Sabit zamanlı karşılaştırma ile imzayı doğrular (timing attack'e karşı).</summary>
    bool Verify(Guid ticketId, string nonce, DateTime expiresAtUtc, string providedSignature);
}
