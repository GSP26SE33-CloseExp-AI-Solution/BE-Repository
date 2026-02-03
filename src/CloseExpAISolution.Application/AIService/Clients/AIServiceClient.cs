using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CloseExpAISolution.Application.AIService.Configuration;
using CloseExpAISolution.Application.AIService.Interfaces;
using CloseExpAISolution.Application.AIService.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CloseExpAISolution.Application.AIService.Clients;

/// <summary>
/// HTTP client for AI Service communication with retry, caching, and circuit breaker
/// </summary>
public class AIServiceClient : IAIServiceClient, IAIServiceBatchClient
{
    private readonly HttpClient _httpClient;
    private readonly AIServiceSettings _settings;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AIServiceClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    // Circuit breaker state
    private int _failureCount;
    private DateTime _circuitOpenTime;
    private readonly object _circuitLock = new();

    public AIServiceClient(
        HttpClient httpClient,
        IOptions<AIServiceSettings> settings,
        IMemoryCache cache,
        ILogger<AIServiceClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _cache = cache;
        _logger = logger;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
        };
    }

    #region Health Checks

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI Service health check failed");
            return false;
        }
    }

    public async Task<ReadyResponse?> CheckReadinessAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/ready", cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ReadyResponse>(_jsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI Service readiness check failed");
            return null;
        }
    }

    public async Task<ServiceInfoResponse?> GetServiceInfoAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/info", cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ServiceInfoResponse>(_jsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI Service info request failed");
            return null;
        }
    }

    #endregion

    #region OCR Operations

    public Task<OcrResponse?> ExtractFromUrlAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        return ExtractAsync(new OcrRequest { ImageUrl = imageUrl }, cancellationToken);
    }

    public Task<OcrResponse?> ExtractFromBase64Async(string imageBase64, CancellationToken cancellationToken = default)
    {
        return ExtractAsync(new OcrRequest { ImageB64 = imageBase64 }, cancellationToken);
    }

    public async Task<OcrResponse?> ExtractAsync(OcrRequest request, CancellationToken cancellationToken = default)
    {
        ValidateOcrRequest(request);

        return await ExecuteWithResilienceAsync<OcrResponse>(
            "/v1/ocr/extract",
            request,
            cancellationToken);
    }

    #endregion

    #region Pricing Operations

    public Task<PricingResponse?> GetPriceSuggestionAsync(
        string productType,
        int daysToExpire,
        decimal basePrice,
        CancellationToken cancellationToken = default)
    {
        return GetPriceSuggestionAsync(new PricingRequest
        {
            ProductType = productType,
            DaysToExpire = daysToExpire,
            BasePrice = basePrice
        }, cancellationToken);
    }

    public async Task<PricingResponse?> GetPriceSuggestionAsync(
        PricingRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidatePricingRequest(request);

        // Try cache first
        string cacheKey = GeneratePricingCacheKey(request);
        if (_settings.EnableCaching && _cache.TryGetValue(cacheKey, out PricingResponse? cached))
        {
            _logger.LogDebug("Pricing cache hit for key: {CacheKey}", cacheKey);
            return cached;
        }

        var response = await ExecuteWithResilienceAsync<PricingResponse>(
            "/v1/pricing/suggest",
            request,
            cancellationToken);

        // Cache successful response
        if (response != null && _settings.EnableCaching)
        {
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(_settings.PricingCacheDurationMinutes));
            _cache.Set(cacheKey, response, cacheOptions);
        }

        return response;
    }

    #endregion

    #region Vision Operations

    public Task<VisionResponse?> AnalyzeImageFromUrlAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        return AnalyzeImageAsync(new VisionRequest { ImageUrl = imageUrl }, cancellationToken);
    }

    public async Task<VisionResponse?> AnalyzeImageAsync(VisionRequest request, CancellationToken cancellationToken = default)
    {
        ValidateVisionRequest(request);

        return await ExecuteWithResilienceAsync<VisionResponse>(
            "/v1/vision/analyze",
            request,
            cancellationToken);
    }

    public async Task<byte[]?> GetAnnotatedImageAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        if (IsCircuitOpen())
        {
            _logger.LogWarning("Circuit breaker is open, skipping annotated image request");
            return null;
        }

        try
        {
            var request = new VisionRequest { ImageUrl = imageUrl };
            var content = new StringContent(
                JsonSerializer.Serialize(request, _jsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("/v1/vision/analyze/annotated", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            RecordSuccess();
            return await response.Content.ReadAsByteArrayAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            RecordFailure();
            _logger.LogError(ex, "Failed to get annotated image");
            return null;
        }
    }

    #endregion

    #region Batch Operations

    public async Task<IEnumerable<OcrResponse?>> ExtractBatchAsync(
        IEnumerable<string> imageUrls,
        int maxConcurrency = 3,
        CancellationToken cancellationToken = default)
    {
        var semaphore = new SemaphoreSlim(maxConcurrency);
        var tasks = imageUrls.Select(async url =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                return await ExtractFromUrlAsync(url, cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        });

        return await Task.WhenAll(tasks);
    }

    public async Task<IEnumerable<PricingResponse?>> GetPriceSuggestionsBatchAsync(
        IEnumerable<PricingRequest> requests,
        int maxConcurrency = 5,
        CancellationToken cancellationToken = default)
    {
        var semaphore = new SemaphoreSlim(maxConcurrency);
        var tasks = requests.Select(async req =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                return await GetPriceSuggestionAsync(req, cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        });

        return await Task.WhenAll(tasks);
    }

    #endregion

    #region Private Methods

    private async Task<T?> ExecuteWithResilienceAsync<T>(
        string endpoint,
        object request,
        CancellationToken cancellationToken) where T : class
    {
        if (IsCircuitOpen())
        {
            _logger.LogWarning("Circuit breaker is open, request to {Endpoint} rejected", endpoint);
            throw new InvalidOperationException("AI Service is currently unavailable (circuit breaker open)");
        }

        Exception? lastException = null;

        for (int attempt = 0; attempt <= _settings.RetryCount; attempt++)
        {
            try
            {
                if (attempt > 0)
                {
                    var delay = TimeSpan.FromMilliseconds(_settings.RetryDelayMs * Math.Pow(2, attempt - 1));
                    _logger.LogDebug("Retry attempt {Attempt} for {Endpoint} after {Delay}ms", attempt, endpoint, delay.TotalMilliseconds);
                    await Task.Delay(delay, cancellationToken);
                }

                var response = await SendRequestAsync<T>(endpoint, request, cancellationToken);
                RecordSuccess();
                return response;
            }
            catch (HttpRequestException ex) when (IsTransientError(ex))
            {
                lastException = ex;
                _logger.LogWarning(ex, "Transient error on attempt {Attempt} for {Endpoint}", attempt + 1, endpoint);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                lastException = ex;
                _logger.LogWarning("Request timeout on attempt {Attempt} for {Endpoint}", attempt + 1, endpoint);
            }
            catch (Exception ex)
            {
                RecordFailure();
                _logger.LogError(ex, "Non-transient error calling {Endpoint}", endpoint);
                throw;
            }
        }

        RecordFailure();
        throw new HttpRequestException($"Failed to call {endpoint} after {_settings.RetryCount + 1} attempts", lastException);
    }

    private async Task<T?> SendRequestAsync<T>(string endpoint, object request, CancellationToken cancellationToken) where T : class
    {
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        
        if (_settings.EnableLogging)
        {
            _logger.LogDebug("AI Service Request to {Endpoint}: {Request}", endpoint, json);
        }

        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (_settings.EnableLogging)
        {
            _logger.LogDebug("AI Service Response from {Endpoint} ({StatusCode}): {Response}", 
                endpoint, response.StatusCode, responseBody);
        }

        if (!response.IsSuccessStatusCode)
        {
            var error = JsonSerializer.Deserialize<AIServiceError>(responseBody, _jsonOptions);
            var errorMessage = error?.Error?.Message ?? $"AI Service returned {response.StatusCode}";
            
            _logger.LogError("AI Service error from {Endpoint}: {ErrorCode} - {ErrorMessage}", 
                endpoint, error?.Error?.Code, errorMessage);
            
            throw new HttpRequestException(errorMessage, null, response.StatusCode);
        }

        return JsonSerializer.Deserialize<T>(responseBody, _jsonOptions);
    }

    private static bool IsTransientError(HttpRequestException ex)
    {
        return ex.StatusCode is 
            HttpStatusCode.RequestTimeout or
            HttpStatusCode.BadGateway or
            HttpStatusCode.ServiceUnavailable or
            HttpStatusCode.GatewayTimeout or
            null; // Network errors
    }

    private bool IsCircuitOpen()
    {
        if (!_settings.EnableCircuitBreaker)
            return false;

        lock (_circuitLock)
        {
            if (_failureCount >= _settings.CircuitBreakerThreshold)
            {
                if (DateTime.UtcNow - _circuitOpenTime < TimeSpan.FromSeconds(_settings.CircuitBreakerDurationSeconds))
                {
                    return true;
                }
                // Half-open state - allow one request
                _failureCount = _settings.CircuitBreakerThreshold - 1;
            }
            return false;
        }
    }

    private void RecordSuccess()
    {
        if (!_settings.EnableCircuitBreaker) return;

        lock (_circuitLock)
        {
            _failureCount = 0;
        }
    }

    private void RecordFailure()
    {
        if (!_settings.EnableCircuitBreaker) return;

        lock (_circuitLock)
        {
            _failureCount++;
            if (_failureCount >= _settings.CircuitBreakerThreshold)
            {
                _circuitOpenTime = DateTime.UtcNow;
                _logger.LogWarning("Circuit breaker opened after {FailureCount} failures", _failureCount);
            }
        }
    }

    private static string GeneratePricingCacheKey(PricingRequest request)
    {
        return $"pricing:{request.ProductType}:{request.DaysToExpire}:{request.BasePrice}:{request.Strategy}";
    }

    #endregion

    #region Validation

    private void ValidateOcrRequest(OcrRequest request)
    {
        if (string.IsNullOrEmpty(request.ImageUrl) && string.IsNullOrEmpty(request.ImageB64))
            throw new ArgumentException("Either ImageUrl or ImageB64 must be provided");
    }

    private void ValidatePricingRequest(PricingRequest request)
    {
        if (string.IsNullOrEmpty(request.ProductType))
            throw new ArgumentException("ProductType is required");
        
        if (request.DaysToExpire < 0)
            throw new ArgumentException("DaysToExpire cannot be negative");
        
        if (request.BasePrice <= 0)
            throw new ArgumentException("BasePrice must be positive");
    }

    private void ValidateVisionRequest(VisionRequest request)
    {
        if (string.IsNullOrEmpty(request.ImageUrl) && string.IsNullOrEmpty(request.ImageB64))
            throw new ArgumentException("Either ImageUrl or ImageB64 must be provided");
    }

    #endregion
}
