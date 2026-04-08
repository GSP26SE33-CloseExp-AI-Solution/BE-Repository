using CloseExpAISolution.Domain;

namespace CloseExpAISolution.Application.Services.Fulfillment;

/// <summary>
/// Ensures <see cref="Order.DeliveryType"/> matches location fields: PICKUP requires collection,
/// DELIVERY requires saved customer address; no mixed pickup+delivery location on one order.
/// </summary>
public static class OrderDeliveryLocationValidator
{
    /// <param name="normalizedDeliveryType">Must be <see cref="DeliveryMethod.Pickup"/> or <see cref="DeliveryMethod.Delivery"/> (already normalized).</param>
    public static void ValidateOrThrow(string normalizedDeliveryType, Guid? collectionId, Guid? addressId)
    {
        if (normalizedDeliveryType == DeliveryMethod.Pickup)
        {
            if (!collectionId.HasValue)
                throw new InvalidOperationException(
                    "Đơn PICKUP cần collectionId (điểm nhận hàng).");
            if (addressId.HasValue)
                throw new InvalidOperationException(
                    "Đơn PICKUP không được kèm addressId.");
        }
        else if (normalizedDeliveryType == DeliveryMethod.Delivery)
        {
            if (!addressId.HasValue)
                throw new InvalidOperationException(
                    "Đơn DELIVERY cần addressId (địa chỉ giao hàng).");
            if (collectionId.HasValue)
                throw new InvalidOperationException(
                    "Đơn DELIVERY không được kèm collectionId.");
        }
    }
}
