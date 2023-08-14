using InternalApi.Contracts;
using InternalApi.Entities;
using TestGrpc;

namespace InternalApi.Services;

/// <inheritdoc />
public class CurrencyService : ICurrencyService
{
    /// <inheritdoc />
    public Task<Currency> GetCurrency(string currencyCode)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task<Currency> GetDefaultCurrency()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task<Currency> GetCurrencyOnDate(string currencyCode, string date)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task<Settings> GetSettings()
    {
        throw new NotImplementedException();
    }
}