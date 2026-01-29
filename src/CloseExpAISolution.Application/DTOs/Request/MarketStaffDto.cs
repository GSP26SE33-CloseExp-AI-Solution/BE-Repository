using CloseExpAISolution.Domain.Entities;

namespace CloseExpAISolution.Application.DTOs.Request;

public class MarketStaffDto
{
    public Guid MarketStaffId { get; set; }
    public Guid UserId { get; set; }
    public Guid SupermarketId { get; set; }
    public string Position { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public User? User { get; set; }
    public Supermarket? Supermarket { get; set; }
}

public class CreateMarketStaffRequestDto
{
    public Guid UserId { get; set; }
    public Guid SupermarketId { get; set; }
    public string Position { get; set; } = string.Empty;
}

public class UpdateMarketStaffRequestDto
{
    public Guid UserId { get; set; }
    public Guid SupermarketId { get; set; }
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
