using InternalApi.Contracts;
using InternalApi.Entities;
using CurrencyApi;

namespace InternalApi.Services;

/// <inheritdoc />
public class CurrencyService : ICurrencyService
{
    /// <inheritdoc />
    public async Task<Currency> GetCurrency(string currencyCode, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async Task<Currency> GetCurrencyOnDate(string currencyCode, string date, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async Task<Settings> GetSettings(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        throw new NotImplementedException();
    }
}