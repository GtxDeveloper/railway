namespace Tringelty.Core.Interfaces;

public interface IImageService
{
    // Принимает поток файла и имя, возвращает URL загруженной картинки
    Task<string> UploadImageAsync(Stream fileStream, string fileName);
    
    // (Опционально) Удаление старой картинки
    Task DeleteImageAsync(string publicUrl); 
}