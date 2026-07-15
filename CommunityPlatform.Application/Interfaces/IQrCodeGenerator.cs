namespace CommunityPlatform.Application.Interfaces;

/// <summary>
/// QR kod PNG üretimi. Bilet e-postalarında (TicketEvent) kullanılır —
/// mobil uygulamayı açmadan, doğrudan e-postadaki görselden check-in
/// yapılabilmesi için sunucu tarafında rasterize edilmiş bir görsel gerekir.
/// </summary>
public interface IQrCodeGenerator
{
    /// <summary>Verilen metni QR koda dönüştürüp PNG byte dizisi olarak döner.</summary>
    byte[] GeneratePng(string payload, int pixelsPerModule = 10);
}
