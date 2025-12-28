using RenuMate.Enums;

namespace RenuMate.Services.Contracts;

public interface ICurrencyService
{
    Task<Dictionary<Currency, decimal>?> GetRateForDesiredCurrency(Currency to, CancellationToken ct = default);
}