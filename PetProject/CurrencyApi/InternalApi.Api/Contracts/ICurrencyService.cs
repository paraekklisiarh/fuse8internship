using InternalApi.Entities;
using CurrencyApi;

namespace InternalApi.Contracts;

/// <summary>
/// Сервис для контроллера CurrencyController
/// </summary>
public interface ICurrencyService
{
    public Task<Currency> GetCurrency(string currencyCode);

    public Task<Currency> GetDefaultCurrency();

    public Task<Currency> GetCurrencyOnDate(string currencyCode, string date);

    public Task<Settings> GetSettings();
}