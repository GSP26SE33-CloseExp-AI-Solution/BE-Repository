using System.Text;

namespace CloseExpAISolution.Domain;

/// <summary>
/// Canonical delivery mode strings stored in <see cref="Entities.Order.DeliveryType"/>
/// and <see cref="Entities.DeliveryGroup.DeliveryType"/> (aligned with FE: PICKUP / DELIVERY).
/// </summary>
public static class DeliveryMethod
{
    public const string Pickup = "PICKUP";
    public const string Delivery = "DELIVERY";

    /// <summary>
    /// Maps API/legacy values to <see cref="Pickup"/> or <see cref="Delivery"/>.
    /// </summary>
    /// <exception cref="ArgumentException">When null/empty or not recognized.</exception>
    public static string NormalizeOrThrow(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new ArgumentException("DeliveryType is required. Use PICKUP or DELIVERY.", nameof(raw));

        var trimmed = raw.Trim();
        if (string.Equals(trimmed, Pickup, StringComparison.OrdinalIgnoreCase))
            return Pickup;
        if (string.Equals(trimmed, Delivery, StringComparison.OrdinalIgnoreCase))
            return Delivery;

        var key = NormalizeKey(trimmed);
        return key switch
        {
            "pickup" or "collectionpoint" or "storepickup" => Pickup,
            "delivery" or "homedelivery" => Delivery,
            _ => throw new ArgumentException(
                $"Invalid DeliveryType '{raw}'. Use {Pickup}, {Delivery}, or legacy aliases (HomeDelivery, CollectionPoint, Pickup, Delivery).",
                nameof(raw))
        };
    }

    private static string NormalizeKey(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (var c in s.ToLowerInvariant())
        {
            if (c is '_' or '-' or ' ')
                continue;
            sb.Append(c);
        }

        return sb.ToString();
    }
}
