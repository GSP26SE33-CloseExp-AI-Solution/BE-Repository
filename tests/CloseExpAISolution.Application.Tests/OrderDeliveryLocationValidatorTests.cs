using CloseExpAISolution.Application.Services.Fulfillment;
using CloseExpAISolution.Domain;
using Xunit;

namespace CloseExpAISolution.Application.Tests;

public class OrderDeliveryLocationValidatorTests
{
    private static readonly Guid Collection = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Address = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void ValidateOrThrow_Pickup_missing_collection_throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            OrderDeliveryLocationValidator.ValidateOrThrow(DeliveryMethod.Pickup, null, null));
        Assert.Contains("collectionId", ex.Message);
    }

    [Fact]
    public void ValidateOrThrow_Pickup_with_address_throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            OrderDeliveryLocationValidator.ValidateOrThrow(DeliveryMethod.Pickup, Collection, Address));
        Assert.Contains("addressId", ex.Message);
    }

    [Fact]
    public void ValidateOrThrow_Pickup_with_collection_ok()
    {
        OrderDeliveryLocationValidator.ValidateOrThrow(DeliveryMethod.Pickup, Collection, null);
    }

    [Fact]
    public void ValidateOrThrow_Delivery_missing_address_throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            OrderDeliveryLocationValidator.ValidateOrThrow(DeliveryMethod.Delivery, null, null));
        Assert.Contains("addressId", ex.Message);
    }

    [Fact]
    public void ValidateOrThrow_Delivery_with_collection_throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            OrderDeliveryLocationValidator.ValidateOrThrow(DeliveryMethod.Delivery, Collection, Address));
        Assert.Contains("collectionId", ex.Message);
    }

    [Fact]
    public void ValidateOrThrow_Delivery_with_address_ok()
    {
        OrderDeliveryLocationValidator.ValidateOrThrow(DeliveryMethod.Delivery, null, Address);
    }
}
