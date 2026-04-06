using System.Globalization;

namespace CloseExpAISolution.Application.Services.Routing;

/// <summary>
/// Nearest-neighbor + 2-opt on a cost matrix (start index 0, stop indices 1..n).
/// </summary>
public static class DeliveryRoutePlanner
{
    public const int MaxCoordinatesPerRequest = 10;

    public static string FormatCoordinatesPath(IReadOnlyList<(double Latitude, double Longitude)> points)
    {
        return string.Join(";", points.Select(p =>
            $"{p.Longitude.ToString(CultureInfo.InvariantCulture)},{p.Latitude.ToString(CultureInfo.InvariantCulture)}"));
    }

    /// <summary>
    /// Build stop visit order (matrix column indices 1..n) using nearest neighbor, then 2-opt.
    /// </summary>
    public static List<int> BuildOptimizedStopOrder(double?[,] cost, int stopCount)
    {
        if (stopCount <= 0)
            return new List<int>();

        var tour = NearestNeighborTour(cost, stopCount);
        if (stopCount >= 3)
            ImproveWithTwoOpt(cost, tour, maxIterations: 50);
        return tour;
    }

    private static List<int> NearestNeighborTour(double?[,] cost, int stopCount)
    {
        var visited = new bool[stopCount + 1];
        visited[0] = true;
        var tour = new List<int>(stopCount);
        var current = 0;

        for (var step = 0; step < stopCount; step++)
        {
            var bestNext = -1;
            var bestVal = double.PositiveInfinity;
            for (var j = 1; j <= stopCount; j++)
            {
                if (visited[j])
                    continue;
                var c = EdgeCost(cost, current, j);
                if (c < bestVal)
                {
                    bestVal = c;
                    bestNext = j;
                }
            }

            if (bestNext < 0 || double.IsPositiveInfinity(bestVal))
                throw new InvalidOperationException("Không tính được ma trận lộ trình giữa một số điểm (cặp không có route).");

            tour.Add(bestNext);
            visited[bestNext] = true;
            current = bestNext;
        }

        return tour;
    }

    private static void ImproveWithTwoOpt(double?[,] cost, List<int> tour, int maxIterations)
    {
        for (var iter = 0; iter < maxIterations; iter++)
        {
            var improved = false;
            for (var i = 0; i < tour.Count; i++)
            {
                for (var k = i + 1; k < tour.Count; k++)
                {
                    var before = PathCostFromStart(cost, tour);
                    ReverseSegment(tour, i, k);
                    var after = PathCostFromStart(cost, tour);
                    if (after + 1e-6 < before)
                    {
                        improved = true;
                    }
                    else
                    {
                        ReverseSegment(tour, i, k);
                    }
                }
            }

            if (!improved)
                break;
        }
    }

    private static void ReverseSegment(List<int> tour, int i, int j)
    {
        while (i < j)
        {
            (tour[i], tour[j]) = (tour[j], tour[i]);
            i++;
            j--;
        }
    }

    private static double PathCostFromStart(double?[,] cost, IReadOnlyList<int> tour)
    {
        if (tour.Count == 0)
            return 0;
        var sum = EdgeCost(cost, 0, tour[0]);
        for (var i = 0; i < tour.Count - 1; i++)
            sum += EdgeCost(cost, tour[i], tour[i + 1]);
        return sum;
    }

    private static double EdgeCost(double?[,] cost, int from, int to)
    {
        var v = cost[from, to];
        if (v == null || double.IsNaN(v.Value))
            return double.PositiveInfinity;
        return v.Value;
    }
}
