namespace CloseExpAISolution.Application.DTOs.Response;

public class AdminSupermarketStaffDto
{
    public Guid SupermarketStaffId { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public bool IsManager { get; set; }
    public string? EmployeeCodeHint { get; set; }
    public int Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
