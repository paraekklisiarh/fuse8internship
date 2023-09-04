using InternalApi.Dtos;

namespace InternalApi.Contracts;

/// <summary>
///     Сервис взаимодействия со внешним CurrencyAPI
/// </summary>
public interface ICurrencyApi
{
    /// <summary>
    ///     Получает текущий курс для всех валют
    /// </summary>
    /// <param name="baseCurrency">Базовая валюта, относительно которой необходимо получить курс</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Список курсов валют</returns>
    Task<RootCurrencyApiDto> GetAllCurrentCurrenciesAsync(string baseCurrency, CancellationToken cancellationToken);

    /// <summary>
    ///     Получает курс для всех валют, актуальный на <paramref name="date" />
    /// </summary>
    /// <param name="baseCurrency">Базовая валюта, относительно которой необходимо получить курс</param>
    /// <param name="date">Дата, на которую нужно получить курс валют</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Список курсов валют на дату</returns>
    Task<RootCurrencyApiDto> GetAllCurrenciesOnDateAsync(string baseCurrency, DateOnly date,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Наличие доступных токенов
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Возвращает true, если есть свободные токены, в иных случаях false</returns>
    public Task<bool> IsNewRequestsAvailable(CancellationToken cancellationToken);
}