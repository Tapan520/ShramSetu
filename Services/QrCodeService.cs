using QRCoder;

namespace ShramSetu.Services;

public interface IQrCodeService
{
    /// <summary>Returns a Base64-encoded PNG QR code image for the given URL.</summary>
    string GenerateBase64(string url);
}

public class QrCodeService : IQrCodeService
{
    public string GenerateBase64(string url)
    {
        using var generator = new QRCodeGenerator();
        var data   = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(data);
        var bytes  = qrCode.GetGraphic(6);
        return Convert.ToBase64String(bytes);
    }
}
