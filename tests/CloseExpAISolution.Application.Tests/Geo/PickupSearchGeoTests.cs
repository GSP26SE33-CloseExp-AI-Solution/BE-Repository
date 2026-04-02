using CloseExpAISolution.Application.Geo;
using Xunit;

namespace CloseExpAISolution.Application.Tests.Geo;

public class PickupSearchGeoTests
{
    [Fact]
    public void ComputeBoundingBox_HoChiMinh_5km_spansRoughlyFiveKmInDegrees()
    {
        decimal refLat = 10.762622m;
        decimal refLng = 106.660172m;
        const double radiusKm = 5.0;

        var (minLat, maxLat, minLng, maxLng) = PickupSearchGeo.ComputeBoundingBox(refLat, refLng, radiusKm);

        var dLat = (double)(maxLat - minLat);
        var dLng = (double)(maxLng - minLng);

        // Full box height ≈ 2 * (radiusKm / km per deg lat).
        var expectedLatSpan = 2.0 * (radiusKm / PickupSearchGeo.KmPerDegreeLat);
        Assert.InRange(dLat, expectedLatSpan * 0.95, expectedLatSpan * 1.05);
        Assert.InRange(dLng, expectedLatSpan * 0.95, expectedLatSpan * 1.15);
    }

    [Fact]
    public void TwoStepFilter_MatchesFullHaversine_OnSyntheticPointsAroundReference()
    {
        const double refLat = 10.762622;
        const double refLng = 106.660172;
        const double radiusKm = 8.0;

        var (minLat, maxLat, minLng, maxLng) = PickupSearchGeo.ComputeBoundingBox(
            (decimal)refLat,
            (decimal)refLng,
            radiusKm);

        bool InBox(double lat, double lng) =>
            lat >= (double)minLat && lat <= (double)maxLat
            && lng >= (double)minLng && lng <= (double)maxLng;

        var points = new (double Lat, double Lng)[]
        {
            (refLat + 0.01, refLng + 0.01),
            (refLat - 0.05, refLng - 0.05),
            (refLat + 0.2, refLng + 0.2),
            (refLat, refLng),
        };

        var full = points
            .Where(p => PickupSearchGeo.HaversineDistanceKm(refLat, refLng, p.Lat, p.Lng) <= radiusKm)
            .ToList();

        var twoStep = points
            .Where(p => InBox(p.Lat, p.Lng))
            .Where(p => PickupSearchGeo.HaversineDistanceKm(refLat, refLng, p.Lat, p.Lng) <= radiusKm)
            .ToList();

        var fullSorted = full.OrderBy(p => p.Lat).ThenBy(p => p.Lng).ToList();
        var twoSorted = twoStep.OrderBy(p => p.Lat).ThenBy(p => p.Lng).ToList();
        Assert.Equal(fullSorted, twoSorted);
    }

    [Fact]
    public void HaversineDistanceKm_SamePoint_IsZero()
    {
        var d = PickupSearchGeo.HaversineDistanceKm(10.0, 106.0, 10.0, 106.0);
        Assert.InRange(d, 0, 1e-12);
    }
}
