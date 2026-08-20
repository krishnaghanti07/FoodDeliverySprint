using CatalogService.Application.DTOs;
using CatalogService.Application.Services;
using FoodDelivery.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatalogService.API.Controllers;

[ApiController]
[Route("api/catalog/operating-hours")]
public class OperatingHoursController : ControllerBase
{
    private readonly CatalogAppService _catalogService;
    public OperatingHoursController(CatalogAppService catalogService) =>
        _catalogService = catalogService;

    /// <summary>Get operating hours for a restaurant (public)</summary>
    [HttpGet]
    public async Task<IActionResult> GetByRestaurant([FromQuery] Guid restaurantId)
    {
        if (restaurantId == Guid.Empty)
            return BadRequest(ApiResponse<List<OperatingHourDto>>.Fail("Restaurant ID is required."));

        var hours = await _catalogService.GetOperatingHoursAsync(restaurantId);
        return Ok(ApiResponse<List<OperatingHourDto>>.Ok(hours));
    }

    /// <summary>Partner/Admin: Set operating hours for a restaurant</summary>
    [HttpPost]
    [Authorize(Roles = "Partner,Admin")]
    public async Task<IActionResult> SetHours([FromQuery] Guid restaurantId, [FromBody] List<CreateOperatingHourDto> dtos)
    {
        if (restaurantId == Guid.Empty)
            return BadRequest(ApiResponse<string>.Fail("Restaurant ID is required."));

        await _catalogService.SetOperatingHoursAsync(restaurantId, dtos);
        return Ok(ApiResponse<string>.Ok("Operating hours updated successfully."));
    }
}
