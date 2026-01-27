using Auth0.AuthenticationApi;
using Auth0.AuthenticationApi.Models;
using Auth0.Core.Exceptions;
using Auth0.ManagementApi;
using Auth0.ManagementApi.Models;
using Microsoft.Extensions.Caching.Memory;
using RenuMate.Api.Services.Contracts;

namespace RenuMate.Api.Services;

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
                AppMetadata = new { internal_id = internalId.ToString() }
            };

            await managementClient.Users.UpdateAsync(auth0Id, updateRequest, ct);
        }
        catch (RateLimitApiException ex)
        {
            logger.LogError(ex, "Auth0 API Rate Limits exceeded: {Message}", ex.Message);
            throw; 
        }
        catch (ErrorApiException ex)
        {
            logger.LogError(ex, "Auth0 API Error: {Message}. Status: {Status}", ex.Message, ex.StatusCode);
            throw; 
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unknown error during Auth0 sync for user {Auth0Id}", auth0Id);
            throw;
        }
    }

    public async Task SetUserActiveStatusAsync(string auth0Id, bool isActive, CancellationToken ct = default)
    {
        try
        {
            var token = await GetManagementTokenAsync();
            managementClient.UpdateAccessToken(token);

            var updateRequest = new UserUpdateRequest
            {
                AppMetadata = new { is_active = isActive }
            };

            await managementClient.Users.UpdateAsync(auth0Id, updateRequest, ct);
        }
        catch (RateLimitApiException ex)
        {
            logger.LogError(ex, "Auth0 API Rate Limits exceeded: {Message}", ex.Message);
            throw; 
        }
        catch (ErrorApiException ex)
        {
            logger.LogError(ex, "Auth0 API Error: {Message}. Status: {Status}", ex.Message, ex.StatusCode);
            throw; 
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unknown error during Auth0 sync for user {Auth0Id}", auth0Id);
            throw;
        }
    }

    public async Task ResendVerificationEmail(string auth0Id, CancellationToken ct = default)
    {
        try
        {
            var token = await GetManagementTokenAsync();
            managementClient.UpdateAccessToken(token);

            var request = new VerifyEmailJobRequest { UserId = auth0Id };
            await managementClient.Jobs.SendVerificationEmailAsync(request, ct);
        }
        catch (RateLimitApiException ex)
        {
            logger.LogError(ex, "Auth0 API Rate Limits exceeded: {Message}", ex.Message);
            throw; 
        }
        catch (ErrorApiException ex)
        {
            logger.LogError(ex, "Auth0 API Error: {Message}. Status: {Status}", ex.Message, ex.StatusCode);
            throw; 
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unknown error during Auth0 sync for user {Auth0Id}", auth0Id);
            throw;
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