using System.Security.Cryptography;
using System.Text;
using CommunityPlatform.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace CommunityPlatform.Infrastructure.Services;

public class TicketSigningService(IConfiguration configuration) : ITicketSigningService
{
    private byte[] Key => Encoding.UTF8.GetBytes(
        configuration["Tickets:Secret"]
        ?? throw new InvalidOperationException(
            "Tickets:Secret config değeri eksik. appsettings.json / user-secrets'a eklenmeli."));

    public string GenerateNonce()
    {
        var bytes = RandomNumberGenerator.GetBytes(16); // 128-bit
        return Base64UrlEncode(bytes);
    }

    public string Sign(Guid ticketId, string nonce, DateTime expiresAtUtc)
    {
        var payload = BuildPayload(ticketId, nonce, expiresAtUtc);
        using var hmac = new HMACSHA256(Key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Base64UrlEncode(hash);
    }

    public bool Verify(Guid ticketId, string nonce, DateTime expiresAtUtc, string providedSignature)
    {
        if (string.IsNullOrEmpty(providedSignature))
            return false;

        var expected = Sign(ticketId, nonce, expiresAtUtc);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var providedBytes = Encoding.UTF8.GetBytes(providedSignature);

        if (expectedBytes.Length != providedBytes.Length)
            return false;

        // Sabit zamanlı karşılaştırma — erken çıkış yapan bir == kontrolü timing attack'e
        // (imzayı byte byte tahmin etmeye) kapı aralar.
        return CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }

    private static string BuildPayload(Guid ticketId, string nonce, DateTime expiresAtUtc)
        => $"{ticketId:N}.{nonce}.{new DateTimeOffset(expiresAtUtc.ToUniversalTime()).ToUnixTimeSeconds()}";

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
