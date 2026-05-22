using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Domain.Entities;

public class PackagingStaff
{
    public Guid PackagingStaffId { get; set; }
    public Guid UserId { get; set; }
    public Guid SupermarketId { get; set; }
    public PackagingStaffState Status { get; set; } = PackagingStaffState.Active;
    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }
    public Supermarket? Supermarket { get; set; }
}
