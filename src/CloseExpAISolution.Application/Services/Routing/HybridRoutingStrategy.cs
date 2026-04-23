using System.Diagnostics;
using CloseExpAISolution.Application.Mapbox.Interfaces;
using Microsoft.Extensions.Logging;

namespace CloseExpAISolution.Application.Services.Routing;

/// <summary>
/// Hybrid route planner: backend NN+2-opt cho tour nhỏ / metric=distance;
/// Mapbox Optimization v1 cho 4..11 stops khi metric=duration; fallback backend khi
/// Optimization lỗi; throw khi vượt giới hạn.
/// </summary>
public class HybridRoutingStrategy
{
    /// <summary>Tour có &lt;= giá trị này luôn dùng backend (quá nhỏ để gọi Optimization).</summary>
    public const int BackendMaxStops = 3;

    /// <summary>Tổng số stops tối đa mà Hybrid chấp nhận (khớp 1 start + 11 stops = 12 toạ độ).</summary>
    public const int HybridMaxStops = 11;

    public const string StrategyBackend = "backend";
    public const string StrategyOptimization = "mapbox-optimization";

    private readonly IMapboxService _mapboxService;
    private readonly IMapboxOptimizationService _optimizationService;
    private readonly ILogger<HybridRoutingStrategy> _logger;

    public HybridRoutingStrategy(
        IMapboxService mapboxService,
        IMapboxOptimizationService optimizationService,
        ILogger<HybridRoutingStrategy> logger)
    {
        _mapboxService = mapboxService;
        _optimizationService = optimizationService;
        _logger = logger;
    }

    public async Task<HybridRoutingResult> PlanAsync(
        IReadOnlyList<(Guid OrderId, double Lat, double Lng)> stops,
        (double Lat, double Lng) start,
        string metric,
        CancellationToken ct = default)
    {
        if (stops == null || stops.Count == 0)
            throw new ArgumentException("Cần ít nhất 1 stop để lập lộ trình.", nameof(stops));

        if (stops.Count > HybridMaxStops)
        {
            throw new InvalidOperationException(
                $"Số điểm trên lộ trình ({stops.Count}) vượt {HybridMaxStops}. Vui lòng chia nhóm giao hoặc liên hệ quản trị.");
        }

        var normalizedMetric = string.Equals(metric, "duration", StringComparison.OrdinalIgnoreCase)
            ? "duration"
            : "distance";

        var shouldTryOptimization = stops.Count > BackendMaxStops && normalizedMetric == "duration";

        var sw = Stopwatch.StartNew();
        HybridRoutingResult result;
        var fallbackUsed = false;

        if (shouldTryOptimization)
        {
            var optResult = await TryRunOptimizationAsync(stops, start, ct);
            if (optResult != null)
            {
                result = optResult;
            }
            else
            {
                fallbackUsed = true;
                result = await RunBackendAsync(stops, start, normalizedMetric, ct);
            }
        }
        else
        {
            result = await RunBackendAsync(stops, start, normalizedMetric, ct);
        }

        sw.Stop();
        _logger.LogInformation(
            "HybridRouting completed. StopCount={StopCount}, Metric={Metric}, StrategyChosen={Strategy}, FallbackUsed={FallbackUsed}, DurationMs={DurationMs}",
            stops.Count, normalizedMetric, result.StrategyUsed, fallbackUsed, sw.ElapsedMilliseconds);

        return result;
    }

    private async Task<HybridRoutingResult?> TryRunOptimizationAsync(
        IReadOnlyList<(Guid OrderId, double Lat, double Lng)> stops,
        (double Lat, double Lng) start,
        CancellationToken ct)
    {
        var coords = new List<(double Latitude, double Longitude)>(stops.Count + 1)
        {
            (start.Lat, start.Lng)
        };
        foreach (var s in stops)
            coords.Add((s.Lat, s.Lng));

        var opt = await _optimizationService.OptimizeDrivingRouteAsync(coords, startIndex: 0, ct);
        if (opt == null || opt.OrderedStopIndices.Count == 0)
            return null;

        var orderedIds = new List<Guid>(opt.OrderedStopIndices.Count);
        foreach (var inputIdx in opt.OrderedStopIndices)
        {
            if (inputIdx <= 0 || inputIdx > stops.Count)
            {
                _logger.LogWarning(
                    "Optimization returned invalid waypoint index {Index} (stopCount={StopCount}). Fallback to backend.",
                    inputIdx, stops.Count);
                return null;
            }
            orderedIds.Add(stops[inputIdx - 1].OrderId);
        }

        return new HybridRoutingResult(
            orderedIds,
            opt.EncodedPolyline,
            Math.Round(opt.DistanceMeters / 1000d, 3),
            Math.Round(opt.DurationSeconds / 60d, 1),
            StrategyOptimization);
    }

    private async Task<HybridRoutingResult> RunBackendAsync(
        IReadOnlyList<(Guid OrderId, double Lat, double Lng)> stops,
        (double Lat, double Lng) start,
        string metric,
        CancellationToken ct)
    {
        var matrixCoords = new List<(double Latitude, double Longitude)>(stops.Count + 1)
        {
            (start.Lat, start.Lng)
        };
        foreach (var s in stops)
            matrixCoords.Add((s.Lat, s.Lng));

        var matrix = await _mapboxService.GetDrivingMatrixAsync(matrixCoords, ct);
        if (matrix == null)
            throw new InvalidOperationException(
                "Không lấy được ma trận lộ trình từ Mapbox. Kiểm tra cấu hình token hoặc thử lại sau.");

        var cost = metric == "duration" ? matrix.DurationsSeconds : matrix.DistancesMeters;
        List<int> tourIndices;
        try
        {
            tourIndices = DeliveryRoutePlanner.BuildOptimizedStopOrder(cost, stops.Count);
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidOperationException(
                "Không tính được thứ tự điểm: " + ex.Message, ex);
        }

        var orderedStops = tourIndices.Select(idx => stops[idx - 1]).ToList();
        var orderedIds = orderedStops.Select(s => s.OrderId).ToList();

        var waypoints = new List<(double Latitude, double Longitude)>(orderedStops.Count + 1)
        {
            (start.Lat, start.Lng)
        };
        waypoints.AddRange(orderedStops.Select(s => (s.Lat, s.Lng)));
        waypoints = CollapseConsecutiveDuplicateCoordinates(waypoints);

        var route = await _mapboxService.GetDrivingRoutePolylineAsync(waypoints, ct);
        if (route == null)
            throw new InvalidOperationException(
                "Không lấy được đường đi chi tiết từ Mapbox. Thử lại sau.");

        return new HybridRoutingResult(
            orderedIds,
            route.EncodedPolyline,
            Math.Round(route.DistanceMeters / 1000d, 3),
            Math.Round(route.DurationSeconds / 60d, 1),
            StrategyBackend);
    }

    private static List<(double Latitude, double Longitude)> CollapseConsecutiveDuplicateCoordinates(
        IReadOnlyList<(double Latitude, double Longitude)> waypoints)
    {
        var result = new List<(double Latitude, double Longitude)>(waypoints.Count);
        foreach (var p in waypoints)
        {
            if (result.Count == 0)
            {
                result.Add(p);
                continue;
            }

            var last = result[^1];
            if (Math.Abs(last.Latitude - p.Latitude) < 1e-7 && Math.Abs(last.Longitude - p.Longitude) < 1e-7)
                continue;
            result.Add(p);
        }

        if (result.Count < 2 && waypoints.Count >= 2)
            return waypoints.Take(2).ToList();

        return result;
    }
}

public sealed record HybridRoutingResult(
    IReadOnlyList<Guid> OrderedIds,
    string EncodedPolyline,
    double DistanceKm,
    double DurationMinutes,
    string StrategyUsed);
