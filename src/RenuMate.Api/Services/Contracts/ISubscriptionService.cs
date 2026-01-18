namespace RenuMate.Api.Services.Contracts;

public interface ISubscriptionService
{
    Task ProcessSubscriptionRenewalAsync(CancellationToken cancellationToken = default);
}