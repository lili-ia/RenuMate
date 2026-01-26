namespace RenuMate.Api.Services.Contracts;

public interface IAuth0Service
{
    Task UpdateUserInternalIdAsync(string auth0Id, Guid internalId, CancellationToken ct = default);

    Task SetUserActiveStatusAsync(string auth0Id, bool isActive, CancellationToken ct = default);
}