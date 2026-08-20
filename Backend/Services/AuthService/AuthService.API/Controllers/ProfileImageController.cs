using FoodDelivery.Shared.Common;
using FoodDelivery.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.API.Controllers;

[ApiController]
[Route("api/auth/profile-image")]
[Authorize]
public class ProfileImageController : ControllerBase
{
    private readonly ICloudinaryService _cloudinaryService;
    private readonly ILogger<ProfileImageController> _logger;

    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

    public ProfileImageController(ICloudinaryService cloudinaryService, ILogger<ProfileImageController> logger)
    {
        _cloudinaryService = cloudinaryService;
        _logger = logger;
    }

    /// <summary>
    /// Upload profile image for any user role
    /// </summary>
    [HttpPost("upload")]
    public async Task<IActionResult> UploadProfileImage(IFormFile file)
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
            var imageUrl = await _cloudinaryService.UploadImageAsync(stream, file.FileName, "profiles");

            _logger.LogInformation("Profile image uploaded successfully to Cloudinary: {ImageUrl}", imageUrl);

            return Ok(ApiResponse<string>.Ok(imageUrl, "Profile image uploaded successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload profile image");
            return StatusCode(500, ApiResponse<string>.Fail($"Failed to upload image: {ex.Message}"));
        }
    }

    /// <summary>
    /// Delete a profile image from Cloudinary
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> DeleteProfileImage([FromQuery] string imageUrl)
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
                _logger.LogInformation("Profile image deleted successfully from Cloudinary: {ImageUrl}", imageUrl);
                return Ok(ApiResponse<bool>.Ok(true, "Profile image deleted successfully."));
            }

            return BadRequest(ApiResponse<bool>.Fail("Failed to delete image."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete profile image");
            return StatusCode(500, ApiResponse<bool>.Fail($"Failed to delete image: {ex.Message}"));
        }
    }
}
