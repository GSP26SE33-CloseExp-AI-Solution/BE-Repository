using CloseExpAISolution.Application.Services.Routing;
using Xunit;

namespace CloseExpAISolution.Application.Tests;

public class DeliveryRoutePlannerTests
{
    [Fact]
    public void NearestNeighbor_ThreeStops_ReturnsExpectedOrder()
    {
        // Index 0 = start; 1,2,3 = stops. From start nearest is 1; from 1 nearest unvisited is 2; then 3.
        var cost = new double?[,]
        {
            { 0, 10, 50, 50 },
            { 10, 0, 1, 100 },
            { 50, 1, 0, 1 },
            { 50, 100, 1, 0 }
        };

        var tour = DeliveryRoutePlanner.BuildOptimizedStopOrder(cost, stopCount: 3);

        Assert.Equal(new[] { 1, 2, 3 }, tour);
    }

    [Fact]
    public void SingleStop_ReturnsOneIndex()
    {
        var cost = new double?[,]
        {
            { 0, 5 },
            { 5, 0 }
        };

        var tour = DeliveryRoutePlanner.BuildOptimizedStopOrder(cost, stopCount: 1);

        Assert.Single(tour);
        Assert.Equal(1, tour[0]);
    }

    [Fact]
    public void ZeroStops_ReturnsEmpty()
    {
        var cost = new double?[,] { { 0 } };
        var tour = DeliveryRoutePlanner.BuildOptimizedStopOrder(cost, stopCount: 0);
        Assert.Empty(tour);
    }

    [Fact]
    public void NullEdge_ThrowsInvalidOperationException()
    {
        var cost = new double?[,]
        {
            { 0, null },
            { null, 0 }
        };

        Assert.Throws<InvalidOperationException>(() =>
            DeliveryRoutePlanner.BuildOptimizedStopOrder(cost, stopCount: 1));
    }
}
