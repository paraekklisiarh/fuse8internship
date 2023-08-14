using InternalApi.Entities;
using TestGrpc;

namespace InternalApi.Contracts;

/// <summary>
/// Сервис для получения данных из файлового кеша
/// </summary>
public interface ICurrencyCacheFileService
{
    /// <summary>
    ///     Получение из кеша актуального курса валюты указанного типа
    /// </summary>
    /// <param name="currencyType">Тип валюты</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Объект <see cref="CurrencyDTO"/>, содержащий актуальный курс валюты</returns>
    public Task<Currency> GetEntity(CurrencyType currencyType, CancellationToken cancellationToken);

    /// <summary>
    ///     Получение из кеша курса валюты указанного типа на указанную дату
    /// </summary>
    /// <param name="currencyType">Тип валюты</param>
    /// <param name="dateOnly">Дата курса</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Объект <see cref="CurrencyDTO"/>, содержащий курс валюты на указанную дату</returns>
    public Task<Currency> GetEntity(CurrencyType currencyType, DateOnly dateOnly,
        CancellationToken cancellationToken);
}