namespace CloseExpAISolution.Application.DTOs;

/// <summary>Result of resolving supermarket + staff row for the current request.</summary>
public sealed class StaffContextResult
{
    public bool Success { get; init; }
    public Guid? SupermarketId { get; init; }
    public Guid? SupermarketStaffId { get; init; }
    public string? ErrorMessage { get; init; }

    public static StaffContextResult Ok(Guid supermarketId, Guid supermarketStaffId) => new()
    {
        Success = true,
        SupermarketId = supermarketId,
        SupermarketStaffId = supermarketStaffId
    };

    public static StaffContextResult Fail(string message) => new()
    {
        Success = false,
        ErrorMessage = message
    };
}
