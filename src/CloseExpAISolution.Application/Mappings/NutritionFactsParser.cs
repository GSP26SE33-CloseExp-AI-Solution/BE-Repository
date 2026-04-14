using System.Text.Json;

namespace CloseExpAISolution.Application.Mappings;

public static class NutritionFactsParser
{
    private const int MaxPlainTextLength = 4000;

    public static Dictionary<string, string>? Parse(string? nutritionFactsJson)
    {
        if (string.IsNullOrWhiteSpace(nutritionFactsJson))
            return null;

        var trimmed = nutritionFactsJson.Trim();

        if (trimmed.StartsWith("{", StringComparison.Ordinal))
        {
            try
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(trimmed);
                if (dict is { Count: > 0 })
                    return dict;
            }
            catch
            {
                // fall through to JsonDocument / plain text
            }

            try
            {
                using var doc = JsonDocument.Parse(trimmed);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        var v = prop.Value.ValueKind switch
                        {
                            JsonValueKind.String => prop.Value.GetString() ?? "",
                            JsonValueKind.Number => prop.Value.ToString(),
                            JsonValueKind.True => "true",
                            JsonValueKind.False => "false",
                            _ => prop.Value.ToString()
                        };
                        if (!string.IsNullOrWhiteSpace(v))
                            result[prop.Name] = v;
                    }

                    if (result.Count > 0)
                        return result;
                }
            }
            catch
            {
                // treat as plain text below
            }
        }

        var text = trimmed.Length > MaxPlainTextLength
            ? trimmed[..MaxPlainTextLength]
            : trimmed;

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["description"] = text
        };
    }
}
