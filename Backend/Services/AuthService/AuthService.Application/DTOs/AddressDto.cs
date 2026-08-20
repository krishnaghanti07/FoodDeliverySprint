using System.ComponentModel.DataAnnotations;

namespace AuthService.Application.DTOs;

public class AddressDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string FullAddress { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;
    public string? Landmark { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateAddressDto
{
    [Required, MaxLength(50)]
    public string Label { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string FullAddress { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string State { get; set; } = string.Empty;

    [Required, MaxLength(10)]
    public string Pincode { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Landmark { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool IsDefault { get; set; } = false;
}

public class UpdateAddressDto
{
    [Required, MaxLength(50)]
    public string Label { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string FullAddress { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string State { get; set; } = string.Empty;

    [Required, MaxLength(10)]
    public string Pincode { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Landmark { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
