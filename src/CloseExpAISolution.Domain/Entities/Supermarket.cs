using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Domain.Entities;

public class Supermarket
{
    public Guid SupermarketId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string ContactPhone { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public SupermarketState Status { get; set; } = SupermarketState.PendingApproval;
    public DateTime CreatedAt { get; set; }

    /// <summary>Vendor who submitted the registration (set when Status is PendingApproval or after approval).</summary>
    public Guid? ApplicantUserId { get; set; }

    public DateTime? SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public string? AdminReviewNote { get; set; }

    /// <summary>Human-readable application reference for support (e.g. ST-2026-XXXX).</summary>
    public string? ApplicationReference { get; set; }

    public User? ApplicantUser { get; set; }
    public User? ReviewedByUser { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<SupermarketStaff> SupermarketStaffs { get; set; } = new List<SupermarketStaff>();
}
