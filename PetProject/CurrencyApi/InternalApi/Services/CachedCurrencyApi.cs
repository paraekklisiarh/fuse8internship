using InternalApi.Contracts;
using InternalApi.Entities;
using TestGrpc;

namespace InternalApi.Services;

/// <inheritdoc />
public class CachedCurrencyApi : ICachedCurrencyApi
{
    private readonly ILogger<CachedCurrencyApi> _logger;
    private readonly ICurrencyCacheFileService _currencyCacheFileIoService;


    /// <summary>
    /// Конструктор класса CachedCurrencyApi.
    /// </summary>
    /// <param name="logger">Логгер</param>
    /// <param name="currencyCacheFileService">Сервис для получения данных из файлового кеша</param>
    public CachedCurrencyApi(ILogger<CachedCurrencyApi> logger, ICurrencyCacheFileService currencyCacheFileService)
    {
        _logger = logger;
        _currencyCacheFileIoService = currencyCacheFileService;
    }

    /// <summary>
    ///     Получение курса валюты по типу
    /// </summary>
    /// <param name="currencyType">Тип валюты</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="OperationCanceledException"></exception>
    public async Task<Currency> GetCurrentCurrencyAsync(CurrencyType currencyType,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
            return await _currencyCacheFileIoService.GetEntity(currencyType, cancellationToken);

        throw new OperationCanceledException(cancellationToken);
    }

    /// <summary>
    ///     Получение курса валюты на указанную дату.
    /// </summary>
    /// <param name="currencyType">Тип валюты</param>
    /// <param name="date">Дата курса валюты</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Полученный из кеша или API <see cref="CurrencyDTO" /></returns>
    public async Task<Currency> GetCurrencyOnDateAsync(CurrencyType currencyType, DateOnly date,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
            return await _currencyCacheFileIoService.GetEntity(currencyType, date, cancellationToken);

        throw new OperationCanceledException(cancellationToken);
    }
}