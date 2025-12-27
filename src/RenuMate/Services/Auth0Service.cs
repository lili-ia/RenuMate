using Auth0.AuthenticationApi;
using Auth0.AuthenticationApi.Models;
using Auth0.ManagementApi;
using Auth0.ManagementApi.Models;
using Microsoft.Extensions.Caching.Memory;
using RenuMate.Services.Contracts;

namespace RenuMate.Services;

public class Auth0Service(
    IConfiguration config,
    IManagementApiClient managementClient,
    IMemoryCache cache,
    ILogger<Auth0Service> logger)
    : IAuth0Service
{
    public async Task UpdateUserInternalIdAsync(string auth0Id, Guid internalId, CancellationToken ct = default)
    {
        try
        {
            var token = await GetManagementTokenAsync();
            managementClient.UpdateAccessToken(token);

            var updateRequest = new UserUpdateRequest
            {
                AppMetadata = new { internal_id = internalId }
            };

            await managementClient.Users.UpdateAsync(auth0Id, updateRequest, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update Auth0 metadata for user {Auth0Id}", auth0Id);
        }
    }

    private async Task<string> GetManagementTokenAsync()
    {
        const string cacheKey = "Auth0ManagementToken";

        if (cache.TryGetValue(cacheKey, out string? cachedToken))
        {
            if (!string.IsNullOrEmpty(cachedToken))
            {
                return cachedToken;    
            }
        }

        var authClient = new AuthenticationApiClient(new Uri($"https://{config["Auth0:Domain"]}"));
        var tokenResponse = await authClient.GetTokenAsync(new ClientCredentialsTokenRequest
        {
            ClientId = config["Auth0:ClientId"],
            ClientSecret = config["Auth0:ClientSecret"],
            Audience = $"https://{config["Auth0:Domain"]}/api/v2/"
        });

        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromHours(23));

        cache.Set(cacheKey, tokenResponse.AccessToken, cacheOptions);

        return tokenResponse.AccessToken;
    }
}