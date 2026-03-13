namespace CloseExpAISolution.Application.DTOs.Request;

public class QuickApproveRequestDto
{
    public decimal OriginalPrice { get; set; }
    public decimal? FinalPrice { get; set; }
    public string StaffId { get; set; } = string.Empty;
    public bool AcceptAiSuggestion { get; set; } = true;
}
