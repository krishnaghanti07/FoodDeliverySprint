using System.Security.Claims;
using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using FoodDelivery.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.API.Controllers;

[ApiController]
[Route("api/auth/addresses")]
[Authorize]
public class AddressController : ControllerBase
{
    private readonly IAddressService _addressService;

    public AddressController(IAddressService addressService)
    {
        _addressService = addressService;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdClaim!);
    }

    /// <summary>
    /// Get all my addresses
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMyAddresses()
    {
        var userId = GetUserId();
        var addresses = await _addressService.GetMyAddressesAsync(userId);
        return Ok(ApiResponse<List<AddressDto>>.Ok(addresses, "Addresses retrieved successfully"));
    }

    /// <summary>
    /// Get my default address
    /// </summary>
    [HttpGet("default")]
    public async Task<IActionResult> GetDefaultAddress()
    {
        var userId = GetUserId();
        var address = await _addressService.GetDefaultAddressAsync(userId);
        
        if (address == null)
            return Ok(ApiResponse<AddressDto?>.Ok(null, "No default address set"));

        return Ok(ApiResponse<AddressDto>.Ok(address, "Default address retrieved"));
    }

    /// <summary>
    /// Get address by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = GetUserId();
        var address = await _addressService.GetByIdAsync(id, userId);
        
        if (address == null)
            return NotFound(ApiResponse<object>.Fail("Address not found"));

        return Ok(ApiResponse<AddressDto>.Ok(address, "Address retrieved successfully"));
    }

    /// <summary>
    /// Create a new address
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateAddress([FromBody] CreateAddressDto dto)
    {
        var userId = GetUserId();
        var address = await _addressService.CreateAddressAsync(userId, dto);
        return Ok(ApiResponse<AddressDto>.Ok(address, "Address created successfully"));
    }

    /// <summary>
    /// Update an existing address
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAddress(Guid id, [FromBody] UpdateAddressDto dto)
    {
        try
        {
            var userId = GetUserId();
            var address = await _addressService.UpdateAddressAsync(id, userId, dto);
            return Ok(ApiResponse<AddressDto>.Ok(address, "Address updated successfully"));
        }
        catch (UnauthorizedAccessException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Delete an address
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAddress(Guid id)
    {
        try
        {
            var userId = GetUserId();
            await _addressService.DeleteAddressAsync(id, userId);
            return Ok(ApiResponse<object>.Ok(null, "Address deleted successfully"));
        }
        catch (UnauthorizedAccessException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Set an address as default
    /// </summary>
    [HttpPatch("{id}/set-default")]
    public async Task<IActionResult> SetDefaultAddress(Guid id)
    {
        try
        {
            var userId = GetUserId();
            await _addressService.SetDefaultAddressAsync(userId, id);
            return Ok(ApiResponse<object>.Ok(null, "Default address updated successfully"));
        }
        catch (UnauthorizedAccessException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
    }
}
