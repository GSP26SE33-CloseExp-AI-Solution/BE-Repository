namespace CloseExpAISolution.Domain.Enums;

public enum PackagingState
{
    Pending, // Confirm order by packaging staff
    Packaging, // Collect order by packaging staff
    Completed, // Complete packaging by packaging staff
    Failed // Failed to package by packaging staff
}
