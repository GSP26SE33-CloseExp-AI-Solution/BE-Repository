using CloseExpAISolution.Domain;
using Xunit;

namespace CloseExpAISolution.Application.Tests;

public class DeliveryMethodTests
{
    [Theory]
    [InlineData("PICKUP", DeliveryMethod.Pickup)]
    [InlineData("pickup", DeliveryMethod.Pickup)]
    [InlineData("Pickup", DeliveryMethod.Pickup)]
    [InlineData("CollectionPoint", DeliveryMethod.Pickup)]
    [InlineData("storepickup", DeliveryMethod.Pickup)]
    [InlineData("pick_up", DeliveryMethod.Pickup)]
    [InlineData("DELIVERY", DeliveryMethod.Delivery)]
    [InlineData("delivery", DeliveryMethod.Delivery)]
    [InlineData("Delivery", DeliveryMethod.Delivery)]
    [InlineData("HomeDelivery", DeliveryMethod.Delivery)]
    [InlineData("home_delivery", DeliveryMethod.Delivery)]
    public void NormalizeOrThrow_maps_aliases_to_canonical(string raw, string expected) =>
        Assert.Equal(expected, DeliveryMethod.NormalizeOrThrow(raw));

    [Fact]
    public void NormalizeOrThrow_null_throws() =>
        Assert.Throws<ArgumentException>(() => DeliveryMethod.NormalizeOrThrow(null));

    [Fact]
    public void NormalizeOrThrow_whitespace_throws() =>
        Assert.Throws<ArgumentException>(() => DeliveryMethod.NormalizeOrThrow("   "));

    [Fact]
    public void NormalizeOrThrow_unknown_throws() =>
        Assert.Throws<ArgumentException>(() => DeliveryMethod.NormalizeOrThrow("Express"));
}
