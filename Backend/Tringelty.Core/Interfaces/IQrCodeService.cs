namespace Tringelty.Core.Interfaces;

public interface IQrCodeService
{
    // Возвращает массив байтов (файл картинки)
    byte[] GenerateQrCode(string url);
}