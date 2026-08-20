namespace FoodDelivery.Shared.Services;

public interface ICloudinaryService
{
    /// <summary>
    /// Upload an image to Cloudinary and return the secure URL
    /// </summary>
    /// <param name="imageStream">The image file stream</param>
    /// <param name="fileName">The original file name</param>
    /// <param name="folder">Optional folder path in Cloudinary (e.g., "restaurants", "menu-items")</param>
    /// <returns>The secure URL of the uploaded image</returns>
    Task<string> UploadImageAsync(Stream imageStream, string fileName, string? folder = null);

    /// <summary>
    /// Delete an image from Cloudinary using its public ID
    /// </summary>
    /// <param name="imageUrl">The Cloudinary image URL</param>
    /// <returns>True if deletion was successful</returns>
    Task<bool> DeleteImageAsync(string imageUrl);
}
