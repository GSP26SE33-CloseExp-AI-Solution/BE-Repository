using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Domain.Entities;

public class SupermarketStaff
{
    public Guid SupermarketStaffId { get; set; }
    public Guid UserId { get; set; }
    public Guid SupermarketId { get; set; }
    public string Position { get; set; } = string.Empty;
    public SupermarketStaffState Status { get; set; } = SupermarketStaffState.Active;
    public DateTime CreatedAt { get; set; }

    public bool IsManager { get; set; }

    /// <summary>BCrypt hash of employee PIN/code for shared-login context selection.</summary>
    public string? EmployeeCodeHash { get; set; }

    /// <summary>Optional hint for UI (e.g. last 4 chars).</summary>
    public string? EmployeeCodeHint { get; set; }

    /// <summary>Reports to manager staff row; null for store manager row.</summary>
    public Guid? ParentSuperStaffId { get; set; }

    public User? User { get; set; }
    public Supermarket? Supermarket { get; set; }
    public SupermarketStaff? ParentSuperStaff { get; set; }
    public ICollection<SupermarketStaff> SubordinateStaff { get; set; } = new List<SupermarketStaff>();
}
