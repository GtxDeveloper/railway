using QRCoder;
using Tringelty.Core.Interfaces;

namespace Tringelty.Infrastructure.Services;

public class QrCodeService : IQrCodeService
{
    public byte[] GenerateQrCode(string url)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        
        // Генерируем PNG (20 пикселей на модуль - высокое качество)
        return qrCode.GetGraphic(20);
    }
}