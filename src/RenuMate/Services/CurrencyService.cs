using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

using RenuMate.Enums;
using RenuMate.Services.Contracts;

namespace RenuMate.Services;

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
            var root = await httpClient.GetFromJsonAsync<JsonElement>(url, ct);

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
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch rates from {Url}", url);
            return null;
        }

        return null;
    }
}