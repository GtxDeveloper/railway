using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using Tringelty.Core.Interfaces;
using Tringelty.Infrastructure.Options;

namespace Tringelty.Infrastructure.Services;

public class CloudinaryService : IImageService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryService(IConfiguration config)
    {
        var options = config.GetSection("CloudinarySettings").Get<CloudinaryOptions>();
        var account = new Account(options.CloudName, options.ApiKey, options.ApiSecret);
        _cloudinary = new Cloudinary(account);
    }

    public async Task<string> UploadImageAsync(Stream fileStream, string fileName)
    {
        if (fileStream.Length == 0) throw new ArgumentException("Empty file");

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, fileStream),
            // Автоматически обрезать под лицо и сделать квадратным 500x500
            Transformation = new Transformation().Width(500).Height(500).Crop("fill").Gravity("face"),
            Folder = "tringelty_avatars" 
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

        if (uploadResult.Error != null)
        {
            throw new Exception(uploadResult.Error.Message);
        }

        return uploadResult.SecureUrl.ToString();
    }

    public async Task DeleteImageAsync(string publicUrl)
    {
        // Cloudinary требует PublicId для удаления. Его нужно вытащить из URL.
        // Это упрощенная логика, для MVP можно пока пропустить удаление старых аватарок.
        await Task.CompletedTask;
    }
}