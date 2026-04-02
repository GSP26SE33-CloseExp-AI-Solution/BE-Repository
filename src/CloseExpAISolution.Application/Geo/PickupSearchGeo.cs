namespace CloseExpAISolution.Application.Geo;

/// <summary>
/// Bounding-box prefilter + Haversine helpers for pickup / collection-point search.
/// </summary>
public static class PickupSearchGeo
{
    /// <summary>Approximate km per degree of latitude (WGS84 mid-latitude).</summary>
    public const double KmPerDegreeLat = 111.32;

    /// <summary>
    /// Axis-aligned bounding box containing a circle of <paramref name="radiusKm"/> around the reference point.
    /// </summary>
    public static (decimal minLat, decimal maxLat, decimal minLng, decimal maxLng) ComputeBoundingBox(
        decimal refLatDecimal,
        decimal refLngDecimal,
        double radiusKm)
    {
        var refLat = (double)refLatDecimal;
        var refLng = (double)refLngDecimal;
        refLat = Math.Clamp(refLat, -89.9, 89.9);

        var deltaLat = radiusKm / KmPerDegreeLat;
        var cosLat = Math.Cos(refLat * (Math.PI / 180.0));
        if (cosLat < 1e-6)
            cosLat = 1e-6;
        var deltaLng = radiusKm / (KmPerDegreeLat * cosLat);

        var minLatD = refLat - deltaLat;
        var maxLatD = refLat + deltaLat;
        var minLngD = refLng - deltaLng;
        var maxLngD = refLng + deltaLng;

        minLatD = Math.Clamp(minLatD, -90.0, 90.0);
        maxLatD = Math.Clamp(maxLatD, -90.0, 90.0);
        minLngD = Math.Clamp(minLngD, -180.0, 180.0);
        maxLngD = Math.Clamp(maxLngD, -180.0, 180.0);

        return ((decimal)minLatD, (decimal)maxLatD, (decimal)minLngD, (decimal)maxLngD);
    }

    public static double HaversineDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthKm = 6371.0;
        static double ToRad(double deg) => deg * (Math.PI / 180.0);

        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthKm * c;
    }
}
