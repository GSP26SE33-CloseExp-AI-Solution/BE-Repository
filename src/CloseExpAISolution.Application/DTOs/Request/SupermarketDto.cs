using System.ComponentModel.DataAnnotations;
using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Application.DTOs.Request;

public class SupermarketDto
{
    public Guid SupermarketId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string ContactPhone { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateSupermarketRequestDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Address { get; set; } = string.Empty;

    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }

    [Required]
    [Phone]
    public string ContactPhone { get; set; } = string.Empty;
}

public class UpdateSupermarketRequestDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Address { get; set; } = string.Empty;

    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }

    [Required]
    [Phone]
    public string ContactPhone { get; set; } = string.Empty;

    public UserState Status { get; set; }
}
