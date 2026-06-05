using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Application.DTOs.Response;

public class AdminSupermarketListItemDto
{
    public Guid SupermarketId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string ContactPhone { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public SupermarketState Status { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>User ID của quản lý (SupermarketStaff với IsManager = true)</summary>
    public Guid? ManagerUserId { get; set; }
    /// <summary>Email tài khoản quản lý</summary>
    public string? ManagerEmail { get; set; }
    /// <summary>Họ tên quản lý</summary>
    public string? ManagerFullName { get; set; }
}
