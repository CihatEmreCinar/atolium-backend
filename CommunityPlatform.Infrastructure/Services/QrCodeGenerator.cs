using CommunityPlatform.Application.Interfaces;
using QRCoder;

namespace CommunityPlatform.Infrastructure.Services;

/// <summary>
/// QRCoder'ın PngByteQRCode renderer'ını kullanır — System.Drawing'e bağımlı
/// DEĞİLDİR, bu yüzden Linux container'larda (libgdiplus vb.) ek native
/// bağımlılık gerektirmez. ECC seviyesi Q (25% hata düzeltme) seçildi:
/// mobil kamerayla farklı açı/ışık koşullarında okunabilirliği artırır.
/// </summary>
public class QrCodeGenerator : IQrCodeGenerator
{
    public byte[] GeneratePng(string payload, int pixelsPerModule = 10)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var pngQrCode = new PngByteQRCode(data);
        return pngQrCode.GetGraphic(pixelsPerModule);
    }
}
