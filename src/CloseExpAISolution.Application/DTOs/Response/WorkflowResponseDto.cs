namespace CloseExpAISolution.Application.DTOs.Response;

public class WorkflowSummaryDto
{
    public int DraftCount { get; set; }
    public int VerifiedCount { get; set; }
    public int PricedCount { get; set; }
    public int PublishedCount { get; set; }
    public int ExpiredCount { get; set; }
    public int TotalCount { get; set; }
}
