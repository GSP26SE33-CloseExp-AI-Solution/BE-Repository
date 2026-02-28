using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Application.DTOs.Response;

public class SupermarketResponseDto
{
    public Guid SupermarketId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string ContactPhone { get; set; } = string.Empty;
    public UserState Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
