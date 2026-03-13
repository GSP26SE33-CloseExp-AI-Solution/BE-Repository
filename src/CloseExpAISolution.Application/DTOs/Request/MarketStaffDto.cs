using System.ComponentModel.DataAnnotations;

namespace CloseExpAISolution.Application.DTOs.Request;

public class MarketStaffDto
{
    public Guid MarketStaffId { get; set; }
    public Guid UserId { get; set; }
    public Guid SupermarketId { get; set; }
    public string Position { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateMarketStaffRequestDto
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid SupermarketId { get; set; }

    [Required]
    [StringLength(100)]
    public string Position { get; set; } = string.Empty;
}

public class UpdateMarketStaffRequestDto
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid SupermarketId { get; set; }

    [Required]
    [StringLength(100)]
    public string Position { get; set; } = string.Empty;
}

public class MarketStaffResponseDto
{
    public Guid MarketStaffId { get; set; }
    public Guid UserId { get; set; }
    public Guid SupermarketId { get; set; }
    public string Position { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
