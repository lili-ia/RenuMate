namespace RenuMate.Services.Contracts;

public interface ISubscriptionService
{
    Task ProcessSubscriptionRenewalAsync(CancellationToken cancellationToken = default);
}