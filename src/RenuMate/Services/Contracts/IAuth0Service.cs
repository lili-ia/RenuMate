namespace RenuMate.Services.Contracts;

public interface IAuth0Service
{
    Task UpdateUserInternalIdAsync(string auth0Id, Guid internalId, CancellationToken ct = default);
}