using InternalApi.Entities;
using CurrencyApi;

namespace InternalApi.Contracts;

/// <summary>
/// Сервис для контроллера CurrencyController
/// </summary>
public interface ICurrencyService
{
    /// <summary>
    /// Получение текущего курса валюты по указанному коду
    /// </summary>
    /// <param name="currencyCode">Код валюты</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Текущий курс валюты</returns>
    /// <exception cref="OperationCanceledException">Выбрасывается, если операция была отменена.</exception>
    public Task<Currency> GetCurrency(string currencyCode, CancellationToken cancellationToken);

    /// <summary>
    /// Получение курса валюты по указанному коду на указанную дату
    /// </summary>
    /// <param name="currencyCode">Код валюты</param>
    /// <param name="date">Дата курса валюты</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Курс валюты на указанную дату</returns>
    /// <exception cref="OperationCanceledException">Выбрасывается, если операция была отменена.</exception>
    public Task<Currency> GetCurrencyOnDate(string currencyCode, string date, CancellationToken cancellationToken);

    /// <summary>
    /// Получение настроек
    /// </summary>
    /// <returns>Текущие настройки сервера</returns>
    /// <exception cref="OperationCanceledException">Выбрасывается, если операция была отменена.</exception>
    public Task<Settings> GetSettings(CancellationToken cancellationToken);
}