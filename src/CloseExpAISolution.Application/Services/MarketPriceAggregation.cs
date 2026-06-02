using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Application.Services;

public static class MarketPriceAggregation
{
    private const int MaxSourcesToReturn = 10;

    public static string NormalizeStoreKey(string? source, string? storeName)
    {
        var token = (source ?? storeName ?? "unknown").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(token) || token == "unknown")
            return "unknown";

        if (token.Contains("://", StringComparison.Ordinal))
        {
            if (Uri.TryCreate(token, UriKind.Absolute, out var uri))
            {
                token = uri.Host.ToLowerInvariant();
            }
        }

        token = token.Replace("www.", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        return token.Split(':', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? token;
    }

    public static string DisplayStoreName(string storeKey, string? storeName, string? source)
    {
        if (!string.IsNullOrWhiteSpace(storeName))
            return storeName.Trim();
        if (!string.IsNullOrWhiteSpace(source))
            return source.Replace("www.", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        return storeKey;
    }

    public static List<MarketPriceDetail> ToAggregatedDetails(
        IEnumerable<MarketPrice> prices,
        string? productName = null)
    {
        var list = prices
            .Where(p => p.Price > 0 && p.Status == MarketPriceState.Active)
            .ToList();

        if (!string.IsNullOrWhiteSpace(productName))
            list = list.Where(p => IsRelevantProductName(p.ProductName, productName)).ToList();

        var latestPerStore = list
            .GroupBy(p => NormalizeStoreKey(p.Source, p.StoreName))
            .Select(g =>
            {
                var best = g.OrderByDescending(x => x.CollectedAt).ThenByDescending(x => x.Confidence).First();
                var key = g.Key;
                return new MarketPriceDetail
                {
                    Source = best.Source,
                    StoreName = DisplayStoreName(key, best.StoreName, best.Source),
                    Price = best.Price,
                    OriginalPrice = best.OriginalPrice,
                    SourceUrl = best.SourceUrl,
                    IsInStock = best.IsInStock,
                    CollectedAt = best.CollectedAt,
                };
            })
            .ToList();

        return FilterOutliers(latestPerStore)
            .Take(MaxSourcesToReturn)
            .ToList();
    }

    public static MarketPriceResult BuildResult(List<MarketPriceDetail> details)
    {
        if (details.Count == 0)
            return new MarketPriceResult();

        var priceValues = details.Select(d => d.Price).ToList();
        return new MarketPriceResult
        {
            MinPrice = priceValues.Min(),
            MaxPrice = priceValues.Max(),
            AvgPrice = priceValues.Average(),
            SourceCount = details.Count,
            Sources = details.Select(d => d.Source).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            LastUpdated = details.Max(d => d.CollectedAt),
            Details = details.OrderBy(d => d.Price).ToList(),
        };
    }

    private static bool IsRelevantProductName(string? observedName, string expectedName)
    {
        if (string.IsNullOrWhiteSpace(observedName))
            return true;

        var observedTokens = Tokenize(observedName);
        var expectedTokens = Tokenize(expectedName);
        if (expectedTokens.Count == 0)
            return true;

        var overlap = expectedTokens.Count(t => observedTokens.Contains(t));
        if (expectedTokens.Count <= 2)
            return overlap >= 1;

        return overlap >= Math.Min(2, expectedTokens.Count);
    }

    private static HashSet<string> Tokenize(string text)
    {
        return text.ToLowerInvariant()
            .Split([' ', ',', '.', '-', '/', '(', ')'], StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length > 1)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static List<MarketPriceDetail> FilterOutliers(List<MarketPriceDetail> details)
    {
        if (details.Count < 3)
            return details.OrderBy(d => d.Price).ToList();

        var sorted = details.Select(d => d.Price).OrderBy(p => p).ToList();
        var median = sorted[sorted.Count / 2];
        if (median <= 0)
            return details.OrderBy(d => d.Price).ToList();

        var minAllowed = median / 3m;
        var maxAllowed = median * 3m;

        return details
            .Where(d => d.Price >= minAllowed && d.Price <= maxAllowed)
            .OrderBy(d => d.Price)
            .ToList();
    }
}
