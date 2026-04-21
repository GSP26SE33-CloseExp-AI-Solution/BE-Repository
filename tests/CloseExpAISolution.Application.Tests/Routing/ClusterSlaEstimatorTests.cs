using CloseExpAISolution.Application.Services.Routing;
using Xunit;

namespace CloseExpAISolution.Application.Tests.Routing;

public class ClusterSlaEstimatorTests
{
    private static readonly (double Lat, double Lng) Supermarket = (10.7760, 106.7008);

    [Fact]
    public void HaversineKm_TwoIdenticalPoints_IsZero()
    {
        var distance = ClusterSlaEstimator.HaversineKm(10.77, 106.70, 10.77, 106.70);

        Assert.Equal(0, distance, precision: 6);
    }

    [Fact]
    public void HaversineKm_KnownDistance_MatchesApproximateValue()
    {
        // ~1.1 km dọc trục Bắc-Nam tại vĩ tuyến 10.7.
        var distance = ClusterSlaEstimator.HaversineKm(10.7700, 106.7000, 10.7800, 106.7000);

        Assert.InRange(distance, 1.0, 1.2);
    }

    [Fact]
    public void EstimateRouteDurationMinutes_NoStops_IsZero()
    {
        var minutes = ClusterSlaEstimator.EstimateRouteDurationMinutes(
            Supermarket, Array.Empty<(double Lat, double Lng)>());

        Assert.Equal(0, minutes);
    }

    [Fact]
    public void EstimateRouteDurationMinutes_DenseCluster_UnderDefaultSla()
    {
        // 5 điểm trong bán kính ~1.5 km quanh siêu thị → phải nằm dưới SLA 40 phút.
        var stops = new[]
        {
            (10.7765, 106.7012),
            (10.7780, 106.7005),
            (10.7755, 106.6998),
            (10.7792, 106.7020),
            (10.7750, 106.7030)
        };

        var minutes = ClusterSlaEstimator.EstimateRouteDurationMinutes(Supermarket, stops);

        Assert.True(minutes < 40, $"Dense cluster phải dưới SLA 40 phút nhưng ước lượng được {minutes:F1}.");
    }

    [Fact]
    public void EstimateRouteDurationMinutes_SpreadCluster_ExceedsDefaultSla()
    {
        // 5 điểm trải ~4-8 km từ siêu thị → vượt SLA 40 phút (tốc độ đô thị 25 km/h).
        var stops = new[]
        {
            (10.7790, 106.7020),
            (10.8060, 106.7180),
            (10.8120, 106.6880),
            (10.7400, 106.7200),
            (10.8200, 106.6500)
        };

        var minutes = ClusterSlaEstimator.EstimateRouteDurationMinutes(Supermarket, stops);

        Assert.True(minutes > 40, $"Spread cluster phải vượt SLA 40 phút nhưng ước lượng được {minutes:F1}.");
    }

    [Fact]
    public void EstimateRouteDurationMinutes_IncludesPerStopOverhead()
    {
        var stops = new[] { Supermarket };
        var minutes = ClusterSlaEstimator.EstimateRouteDurationMinutes(Supermarket, stops);

        // Khoảng cách ~0 km ⇒ chỉ còn overhead cố định mỗi stop.
        Assert.Equal(ClusterSlaEstimator.PerStopOverheadMinutes, minutes, precision: 4);
    }
}
