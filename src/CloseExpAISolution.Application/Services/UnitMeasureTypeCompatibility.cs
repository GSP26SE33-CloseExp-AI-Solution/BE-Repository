namespace CloseExpAISolution.Application.Services;

public static class UnitMeasureTypeCompatibility
{
    public static bool AreCompatible(string? typeA, string? typeB)
    {
        if (string.IsNullOrWhiteSpace(typeA) || string.IsNullOrWhiteSpace(typeB))
            return false;

        return string.Equals(typeA.Trim(), typeB.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
