using FoodDelivery.Shared.Common;
using FoodDelivery.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatalogService.API.Controllers;

[ApiController]
[Route("api/catalog/images")]
[Authorize(Roles = "Partner,Admin")]
public class ImageUploadController : ControllerBase
{
    private readonly ICloudinaryService _cloudinaryService;
    private readonly ILogger<ImageUploadController> _logger;

    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

    public ImageUploadController(ICloudinaryService cloudinaryService, ILogger<ImageUploadController> logger)
    {
        _cloudinaryService = cloudinaryService;
        _logger = logger;
    }

    /// <summary>
    /// Upload restaurant logo image
    /// </summary>
    [HttpPost("restaurant-logo")]
    public async Task<IActionResult> UploadRestaurantLogo(IFormFile file)
    {
        return await UploadImage(file, "restaurants/logos");
    }

    /// <summary>
    /// Upload menu item image
    /// </summary>
    [HttpPost("menu-item")]
    public async Task<IActionResult> UploadMenuItemImage(IFormFile file)
    {
        return await UploadImage(file, "menu-items");
    }

    private async Task<IActionResult> UploadImage(IFormFile file, string folder)
    {
        try
        {
            // Validate file
            if (file == null || file.Length == 0)
            {
                return BadRequest(ApiResponse<string>.Fail("No file uploaded."));
            }

            // Check file size
            if (file.Length > MaxFileSize)
            {
                return BadRequest(ApiResponse<string>.Fail($"File size exceeds maximum allowed size of {MaxFileSize / 1024 / 1024}MB."));
            }

            // Check file extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                return BadRequest(ApiResponse<string>.Fail($"Invalid file type. Allowed types: {string.Join(", ", AllowedExtensions)}"));
            }

            // Upload to Cloudinary
            using var stream = file.OpenReadStream();
            var imageUrl = await _cloudinaryService.UploadImageAsync(stream, file.FileName, folder);

            _logger.LogInformation("Image uploaded successfully to Cloudinary: {ImageUrl}", imageUrl);

            return Ok(ApiResponse<string>.Ok(imageUrl, "Image uploaded successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload image");
            return StatusCode(500, ApiResponse<string>.Fail($"Failed to upload image: {ex.Message}"));
        }
    }

    /// <summary>
    /// Delete an image from Cloudinary
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> DeleteImage([FromQuery] string imageUrl)
    {
        try
        {
            if (string.IsNullOrEmpty(imageUrl))
            {
                return BadRequest(ApiResponse<bool>.Fail("Image URL is required."));
            }

            var success = await _cloudinaryService.DeleteImageAsync(imageUrl);

            if (success)
            {
                _logger.LogInformation("Image deleted successfully from Cloudinary: {ImageUrl}", imageUrl);
                return Ok(ApiResponse<bool>.Ok(true, "Image deleted successfully."));
            }

            return BadRequest(ApiResponse<bool>.Fail("Failed to delete image."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete image");
            return StatusCode(500, ApiResponse<bool>.Fail($"Failed to delete image: {ex.Message}"));
        }
    }
}
