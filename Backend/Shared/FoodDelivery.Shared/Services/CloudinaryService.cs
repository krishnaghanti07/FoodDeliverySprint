using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;

namespace FoodDelivery.Shared.Services;

public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryService(IConfiguration configuration)
    {
        var cloudName = configuration["Cloudinary:CloudName"];
        var apiKey = configuration["Cloudinary:ApiKey"];
        var apiSecret = configuration["Cloudinary:ApiSecret"];

        if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
        {
            throw new InvalidOperationException("Cloudinary credentials are not configured properly.");
        }

        var account = new Account(cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account);
        _cloudinary.Api.Secure = true;
    }

    public async Task<string> UploadImageAsync(Stream imageStream, string fileName, string? folder = null)
    {
        try
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, imageStream),
                Folder = folder ?? "food-delivery",
                UseFilename = true,
                UniqueFilename = true,
                Overwrite = false
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                throw new Exception($"Cloudinary upload failed: {uploadResult.Error.Message}");
            }

            return uploadResult.SecureUrl.ToString();
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to upload image to Cloudinary: {ex.Message}", ex);
        }
    }

    public async Task<bool> DeleteImageAsync(string imageUrl)
    {
        try
        {
            if (string.IsNullOrEmpty(imageUrl) || !imageUrl.Contains("cloudinary.com"))
            {
                return false;
            }

            // Extract public ID from URL
            var publicId = ExtractPublicIdFromUrl(imageUrl);
            if (string.IsNullOrEmpty(publicId))
            {
                return false;
            }

            var deletionParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deletionParams);

            return result.Result == "ok";
        }
        catch
        {
            return false;
        }
    }

    private string ExtractPublicIdFromUrl(string imageUrl)
    {
        try
        {
            // URL format: https://res.cloudinary.com/{cloud_name}/image/upload/v{version}/{folder}/{public_id}.{format}
            var uri = new Uri(imageUrl);
            var segments = uri.AbsolutePath.Split('/');

            // Find the index of "upload"
            var uploadIndex = Array.IndexOf(segments, "upload");
            if (uploadIndex == -1 || uploadIndex >= segments.Length - 1)
            {
                return string.Empty;
            }

            // Get everything after "upload" and version (if present)
            var startIndex = uploadIndex + 1;
            if (segments[startIndex].StartsWith("v"))
            {
                startIndex++; // Skip version segment
            }

            // Join remaining segments and remove file extension
            var publicIdWithExtension = string.Join("/", segments.Skip(startIndex));
            var lastDotIndex = publicIdWithExtension.LastIndexOf('.');
            return lastDotIndex > 0 ? publicIdWithExtension.Substring(0, lastDotIndex) : publicIdWithExtension;
        }
        catch
        {
            return string.Empty;
        }
    }
}
