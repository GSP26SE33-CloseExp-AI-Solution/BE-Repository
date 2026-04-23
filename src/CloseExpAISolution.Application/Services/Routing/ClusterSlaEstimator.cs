namespace CloseExpAISolution.Application.Services.Routing;

/// <summary>
/// Ước lượng nhanh duration cho một cluster delivery để quyết định SLA split khi
/// generate-draft: dùng greedy Nearest-Neighbor trên haversine + vận tốc đô thị
/// trung bình 25 km/h + overhead 1 phút/stop. Đủ chính xác làm guard, không thay
/// thế Mapbox Matrix ở giai đoạn route-plan thật.
/// </summary>
public static class ClusterSlaEstimator
{
    public const double UrbanMinutesPerKm = 60d / 25d;
    public const double PerStopOverheadMinutes = 1d;

    public static double EstimateRouteDurationMinutes(
        (double Lat, double Lng) origin,
        IReadOnlyList<(double Lat, double Lng)> stops)
    {
        if (stops == null || stops.Count == 0)
            return 0d;

        var visited = new bool[stops.Count];
        var totalMinutes = 0d;
        var currentLat = origin.Lat;
        var currentLng = origin.Lng;

        for (var step = 0; step < stops.Count; step++)
        {
            var bestIdx = -1;
            var bestKm = double.PositiveInfinity;
            for (var i = 0; i < stops.Count; i++)
            {
                if (visited[i])
                    continue;
                var d = HaversineKm(currentLat, currentLng, stops[i].Lat, stops[i].Lng);
                if (d < bestKm)
                {
                    bestKm = d;
                    bestIdx = i;
                }
            }

            if (bestIdx < 0)
                break;

            totalMinutes += bestKm * UrbanMinutesPerKm + PerStopOverheadMinutes;
            visited[bestIdx] = true;
            currentLat = stops[bestIdx].Lat;
            currentLng = stops[bestIdx].Lng;
        }

        return totalMinutes;
    }

    public static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double r = 6371d;
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
                * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return r * c;
    }

    private static double ToRad(double degree) => degree * Math.PI / 180d;
}
