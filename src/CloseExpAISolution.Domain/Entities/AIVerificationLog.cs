namespace CloseExpAISolution.Domain.Entities;

public class AIVerificationLog
{
    public Guid VerificationId { get; set; }
    public Guid ProductId { get; set; }
    public string ExtractedName { get; set; } = string.Empty;
    public DateTime? ExtractedExpiryDate { get; set; }
    public string ExtractedBarcode { get; set; } = string.Empty;
    public decimal ConfidenceScore { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? VerifiedBy { get; set; }


    public Product? Product { get; set; }
}

