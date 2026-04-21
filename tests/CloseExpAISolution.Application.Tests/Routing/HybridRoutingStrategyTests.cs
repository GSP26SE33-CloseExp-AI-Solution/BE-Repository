using CloseExpAISolution.Application.Mapbox.DTOs;
using CloseExpAISolution.Application.Mapbox.Interfaces;
using CloseExpAISolution.Application.Services.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CloseExpAISolution.Application.Tests.Routing;

public class HybridRoutingStrategyTests
{
    private static (Guid OrderId, double Lat, double Lng) Stop(double lat, double lng) =>
        (Guid.NewGuid(), lat, lng);

    [Fact]
    public async Task SmallTour_DistanceMetric_UsesBackendAndDoesNotCallOptimization()
    {
        var stops = new[] { Stop(10.77, 106.70), Stop(10.78, 106.71), Stop(10.79, 106.72) };

        var mapbox = new FakeMapboxService(matrixFactory: n => BuildSymmetricMatrix(n));
        var opt = new FakeOptimizationService();

        var strategy = new HybridRoutingStrategy(mapbox, opt, NullLogger<HybridRoutingStrategy>.Instance);

        var result = await strategy.PlanAsync(stops, (10.77, 106.70), metric: "distance");

        Assert.Equal(HybridRoutingStrategy.StrategyBackend, result.StrategyUsed);
        Assert.Equal(3, result.OrderedIds.Count);
        Assert.Equal("ENCODED", result.EncodedPolyline);
        Assert.Equal(0, opt.CallCount);
        Assert.Equal(1, mapbox.MatrixCalls);
        Assert.Equal(1, mapbox.DirectionsCalls);
    }

    [Fact]
    public async Task MediumTour_DurationMetric_UsesMapboxOptimization()
    {
        var stops = Enumerable.Range(0, 5)
            .Select(i => Stop(10.77 + i * 0.01, 106.70 + i * 0.01))
            .ToArray();

        var mapbox = new FakeMapboxService(matrixFactory: n => BuildSymmetricMatrix(n));
        var opt = new FakeOptimizationService((coords, _) =>
        {
            var stopCount = coords.Count - 1;
            var ordered = Enumerable.Range(1, stopCount).Reverse().ToList();
            return new OptimizedTripResultDto
            {
                OrderedStopIndices = ordered,
                EncodedPolyline = "OPT-POLYLINE",
                DistanceMeters = 5000,
                DurationSeconds = 600
            };
        });

        var strategy = new HybridRoutingStrategy(mapbox, opt, NullLogger<HybridRoutingStrategy>.Instance);

        var result = await strategy.PlanAsync(stops, (10.77, 106.70), metric: "duration");

        Assert.Equal(HybridRoutingStrategy.StrategyOptimization, result.StrategyUsed);
        Assert.Equal(stops.Length, result.OrderedIds.Count);
        Assert.Equal(stops[^1].OrderId, result.OrderedIds[0]);
        Assert.Equal(stops[0].OrderId, result.OrderedIds[^1]);
        Assert.Equal("OPT-POLYLINE", result.EncodedPolyline);
        Assert.Equal(0, mapbox.MatrixCalls);
        Assert.Equal(0, mapbox.DirectionsCalls);
        Assert.Equal(1, opt.CallCount);
    }

    [Fact]
    public async Task MediumTour_DistanceMetric_StillUsesBackend()
    {
        var stops = Enumerable.Range(0, 5)
            .Select(i => Stop(10.77 + i * 0.01, 106.70 + i * 0.01))
            .ToArray();

        var mapbox = new FakeMapboxService(matrixFactory: n => BuildSymmetricMatrix(n));
        var opt = new FakeOptimizationService();

        var strategy = new HybridRoutingStrategy(mapbox, opt, NullLogger<HybridRoutingStrategy>.Instance);

        var result = await strategy.PlanAsync(stops, (10.77, 106.70), metric: "distance");

        Assert.Equal(HybridRoutingStrategy.StrategyBackend, result.StrategyUsed);
        Assert.Equal(0, opt.CallCount);
    }

    [Fact]
    public async Task OptimizationReturnsNull_FallsBackToBackend()
    {
        var stops = Enumerable.Range(0, 6)
            .Select(i => Stop(10.77 + i * 0.01, 106.70 + i * 0.01))
            .ToArray();

        var mapbox = new FakeMapboxService(matrixFactory: n => BuildSymmetricMatrix(n));
        var opt = new FakeOptimizationService((_, _) => null);

        var strategy = new HybridRoutingStrategy(mapbox, opt, NullLogger<HybridRoutingStrategy>.Instance);

        var result = await strategy.PlanAsync(stops, (10.77, 106.70), metric: "duration");

        Assert.Equal(HybridRoutingStrategy.StrategyBackend, result.StrategyUsed);
        Assert.Equal(1, opt.CallCount);
        Assert.Equal(1, mapbox.MatrixCalls);
        Assert.Equal(1, mapbox.DirectionsCalls);
    }

    [Fact]
    public async Task OverLimitStops_ThrowsBeforeCallingAnyApi()
    {
        var stops = Enumerable.Range(0, HybridRoutingStrategy.HybridMaxStops + 2)
            .Select(i => Stop(10.77 + i * 0.001, 106.70 + i * 0.001))
            .ToArray();

        var mapbox = new FakeMapboxService(matrixFactory: n => BuildSymmetricMatrix(n));
        var opt = new FakeOptimizationService();

        var strategy = new HybridRoutingStrategy(mapbox, opt, NullLogger<HybridRoutingStrategy>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            strategy.PlanAsync(stops, (10.77, 106.70), metric: "duration"));

        Assert.Contains("vượt", ex.Message);
        Assert.Equal(0, mapbox.MatrixCalls);
        Assert.Equal(0, opt.CallCount);
    }

    [Fact]
    public async Task EmptyStops_Throws()
    {
        var mapbox = new FakeMapboxService(matrixFactory: n => BuildSymmetricMatrix(n));
        var opt = new FakeOptimizationService();
        var strategy = new HybridRoutingStrategy(mapbox, opt, NullLogger<HybridRoutingStrategy>.Instance);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            strategy.PlanAsync(Array.Empty<(Guid, double, double)>(), (10.77, 106.70), metric: "duration"));
    }

    [Fact]
    public async Task OptimizationReturnsInvalidIndex_FallsBackToBackend()
    {
        var stops = Enumerable.Range(0, 5)
            .Select(i => Stop(10.77 + i * 0.01, 106.70 + i * 0.01))
            .ToArray();

        var mapbox = new FakeMapboxService(matrixFactory: n => BuildSymmetricMatrix(n));
        var opt = new FakeOptimizationService((coords, _) => new OptimizedTripResultDto
        {
            OrderedStopIndices = new List<int> { 99 },
            EncodedPolyline = "BAD",
            DistanceMeters = 1,
            DurationSeconds = 1
        });

        var strategy = new HybridRoutingStrategy(mapbox, opt, NullLogger<HybridRoutingStrategy>.Instance);

        var result = await strategy.PlanAsync(stops, (10.77, 106.70), metric: "duration");

        Assert.Equal(HybridRoutingStrategy.StrategyBackend, result.StrategyUsed);
        Assert.Equal(1, opt.CallCount);
    }

    private static DrivingMatrixResultDto BuildSymmetricMatrix(int n)
    {
        var dist = new double?[n, n];
        var dur = new double?[n, n];
        for (var i = 0; i < n; i++)
            for (var j = 0; j < n; j++)
            {
                dist[i, j] = i == j ? 0 : Math.Abs(i - j) * 100d;
                dur[i, j] = i == j ? 0 : Math.Abs(i - j) * 60d;
            }
        return new DrivingMatrixResultDto { Size = n, DistancesMeters = dist, DurationsSeconds = dur };
    }

    private sealed class FakeMapboxService : IMapboxService
    {
        private readonly Func<int, DrivingMatrixResultDto?> _matrixFactory;

        public FakeMapboxService(Func<int, DrivingMatrixResultDto?> matrixFactory)
        {
            _matrixFactory = matrixFactory;
        }

        public int MatrixCalls { get; private set; }
        public int DirectionsCalls { get; private set; }

        public Task<GeocodingResultDto?> ForwardGeocodeAsync(string address, CancellationToken ct = default) =>
            Task.FromResult<GeocodingResultDto?>(null);

        public Task<GeocodingResultDto?> ReverseGeocodeAsync(double latitude, double longitude, CancellationToken ct = default) =>
            Task.FromResult<GeocodingResultDto?>(null);

        public Task<IEnumerable<GeocodingResultDto>> SearchAddressAsync(string query, int limit = 5, CancellationToken ct = default) =>
            Task.FromResult(Enumerable.Empty<GeocodingResultDto>());

        public Task<double?> GetDrivingDistanceKmAsync(
            double fromLatitude, double fromLongitude, double toLatitude, double toLongitude,
            CancellationToken ct = default) =>
            Task.FromResult<double?>(null);

        public Task<DrivingMatrixResultDto?> GetDrivingMatrixAsync(
            IReadOnlyList<(double Latitude, double Longitude)> coordinates,
            CancellationToken ct = default)
        {
            MatrixCalls++;
            return Task.FromResult(_matrixFactory(coordinates.Count));
        }

        public Task<DrivingRouteGeometryDto?> GetDrivingRoutePolylineAsync(
            IReadOnlyList<(double Latitude, double Longitude)> waypoints,
            CancellationToken ct = default)
        {
            DirectionsCalls++;
            return Task.FromResult<DrivingRouteGeometryDto?>(new DrivingRouteGeometryDto
            {
                EncodedPolyline = "ENCODED",
                DistanceMeters = 1000 * waypoints.Count,
                DurationSeconds = 60 * waypoints.Count
            });
        }
    }

    private sealed class FakeOptimizationService : IMapboxOptimizationService
    {
        private readonly Func<IReadOnlyList<(double Lat, double Lng)>, int?, OptimizedTripResultDto?>? _handler;

        public FakeOptimizationService(
            Func<IReadOnlyList<(double Lat, double Lng)>, int?, OptimizedTripResultDto?>? handler = null)
        {
            _handler = handler;
        }

        public int CallCount { get; private set; }

        public Task<OptimizedTripResultDto?> OptimizeDrivingRouteAsync(
            IReadOnlyList<(double Latitude, double Longitude)> coordinates,
            int? startIndex = 0,
            CancellationToken ct = default)
        {
            CallCount++;
            var mapped = coordinates.Select(c => (c.Latitude, c.Longitude)).ToList();
            var result = _handler?.Invoke(mapped, startIndex);
            return Task.FromResult<OptimizedTripResultDto?>(result);
        }
    }
}
