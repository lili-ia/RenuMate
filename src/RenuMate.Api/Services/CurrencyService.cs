using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Polly;
using RenuMate.Api.Enums;
using RenuMate.Api.Services.Contracts;

namespace RenuMate.Api.Services;

public class CurrencyService(
    HttpClient httpClient, 
    IMemoryCache cache, 
    ILogger<CurrencyService> logger) : ICurrencyService
{
    private const string BaseUrl = "https://cdn.jsdelivr.net/npm/@fawazahmed0/currency-api@latest/v1/currencies";

    public async Task<Dictionary<Currency, decimal>?> GetRateForDesiredCurrency(Currency to, CancellationToken ct)
    {
        var toCode = to.ToString().ToLowerInvariant();
        var cacheKey = $"rates_{toCode}";
    
        if (cache.TryGetValue(cacheKey, out Dictionary<Currency, decimal>? cachedRates))
        {
            return cachedRates;
        }

        var url = $"{BaseUrl}/{toCode}.json";
    
        try
        {
            var policy = Policy
                .Handle<HttpRequestException>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                    onRetry: (ex, timespan, attempt, context) =>
                    {
                        logger.LogWarning(ex, "Retry {Attempt} after {Delay}s due to HttpRequestException", 
                            attempt, timespan.TotalSeconds);
                    });

            var root = await policy.ExecuteAsync(async () 
                => await httpClient.GetFromJsonAsync<JsonElement>(url, ct)); 

            if (root.TryGetProperty(toCode, out var ratesElement))
            {
                var rawRates = ratesElement.Deserialize<Dictionary<string, decimal>>();
            
                if (rawRates == null) return null;

                var mappedRates = new Dictionary<Currency, decimal>();
                foreach (var kvp in rawRates)
                {
                    if (Enum.TryParse<Currency>(kvp.Key, true, out var currencyEnum))
                    {
                        mappedRates[currencyEnum] = kvp.Value;
                    }
                }

                cache.Set(cacheKey, mappedRates, TimeSpan.FromHours(24));
                
                return mappedRates;
            }
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Currency API unreachable: {Url}", url);
            return null;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to parse JSON from {Url}", url);
            throw; 
        }

        return null;
    }
}