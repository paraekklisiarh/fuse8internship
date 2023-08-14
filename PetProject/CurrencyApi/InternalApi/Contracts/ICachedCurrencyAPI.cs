using InternalApi.Entities;
using TestGrpc;

namespace InternalApi.Contracts;

/// <summary>
///     Сервис кеширования данных из внешнего API
/// </summary>
public interface ICachedCurrencyApi
{
    /// <summary>
    ///     Получает текущий курс
    /// </summary>
    /// <param name="currencyType">Валюта, для которой необходимо получить курс</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Текущий курс</returns>
    Task<Currency> GetCurrentCurrencyAsync(CurrencyType currencyType, CancellationToken cancellationToken);

    /// <summary>
    ///     Получает курс валюты, актуальный на <paramref name="date" />
    /// </summary>
    /// <param name="currencyType">Валюта, для которой необходимо получить курс</param>
    /// <param name="date">Дата, на которую нужно получить курс валют</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Курс на дату</returns>
    Task<Currency> GetCurrencyOnDateAsync(CurrencyType currencyType, DateOnly date,
        CancellationToken cancellationToken);
}