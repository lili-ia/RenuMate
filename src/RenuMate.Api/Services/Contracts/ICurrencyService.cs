using RenuMate.Api.Enums;

namespace RenuMate.Api.Services.Contracts;

public interface ICurrencyService
{
    Task<Dictionary<Currency, decimal>?> GetRateForDesiredCurrency(Currency to, CancellationToken ct = default);
}