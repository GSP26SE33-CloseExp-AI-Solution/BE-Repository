using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Application.DTOs.Response;

public class MySupermarketApplicationDto
{
    public Guid SupermarketId { get; set; }
    public string? ApplicationReference { get; set; }
    public SupermarketState Status { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string ContactPhone { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? AdminReviewNote { get; set; }
}

public class AdminPendingSupermarketApplicationDto
{
    public Guid SupermarketId { get; set; }
    public string? ApplicationReference { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string ContactPhone { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public Guid? ApplicantUserId { get; set; }
    public string? ApplicantEmail { get; set; }
    public string? ApplicantFullName { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreatedStaffPersonaDto
{
    public Guid SupermarketStaffId { get; set; }
    public Guid SupermarketId { get; set; }
    public string Position { get; set; } = string.Empty;
    /// <summary>Plain employee code — shown once; store securely.</summary>
    public string EmployeeCode { get; set; } = string.Empty;
    public string? EmployeeCodeHint { get; set; }
}
