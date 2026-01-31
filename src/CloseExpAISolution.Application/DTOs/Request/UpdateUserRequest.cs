namespace CloseExpAISolution.Application.DTOs.Request;

public class UpdateUserRequest
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Status { get; set; }
    public int? RoleId { get; set; }
}
