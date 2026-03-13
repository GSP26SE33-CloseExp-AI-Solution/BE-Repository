namespace CloseExpAISolution.Domain.Entities;

public class CustomerFeedback
{
    public Guid CustomerFeedbackId { get; set; }
    public Guid UserId { get; set; }
    public Guid OrderId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }
    public Order? Order { get; set; }
}

