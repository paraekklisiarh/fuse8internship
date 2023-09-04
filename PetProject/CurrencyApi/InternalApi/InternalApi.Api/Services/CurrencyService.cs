using System.Globalization;
using CurrencyApi;
using InternalApi.Configuration;
using InternalApi.Contracts;
using InternalApi.Dtos;
using InternalApi.Entities;
using InternalApi.Infrastructure;
using InternalApi.Infrastructure.Data.CurrencyContext;
using Microsoft.Extensions.Options;

namespace InternalApi.Services;

/// <summary>
///     Сервис для REST-Api
/// </summary>
public interface ICurrencyService
{
    Task<CurrencyDTO> GetCurrency(CurrencyType currencyType, CancellationToken cancellationToken);

    Task<CurrencyDTO> GetCurrencyOnDate(CurrencyType currencyType, DateOnly date,
        CancellationToken cancellationToken);
    
    
    
    /// <summary>
    ///     Получить текущие настройки приложения
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Текущие настройки приложения</returns>
    Task<Settings> GetCurrencySettings(CancellationToken cancellationToken);

    /// <summary>
    ///     Проверка жизнеспособности внешнего API
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>true, если API жизнеспособно. Иначе false.</returns>
    Task<bool> IsExternalApiHealth(CancellationToken cancellationToken);

    /// <summary>
    ///     Заменить базовую валюту, относительно которой рассчитывается курс валют
    /// </summary>
    /// <param name="newBaseCurrency">Новая базовая валюта</param>
    /// <returns>
    ///     Возвращает из метода идентификатор созданной задачи
    /// </returns>
    Task<Guid> CurrencyConversion(CurrencyType newBaseCurrency);
}

/// <inheritdoc />
public class CurrencyService : ICurrencyService
{
    private readonly ILogger<CurrencyService> _logger;
    private readonly AppDbContext _dbContext;
    private readonly CurrencyCacheSettings _cacheSettings;
    private readonly IInternalQueue<CurrencyConversionTask> _conversionQueue;
    private readonly ICurrencyApi _currencyApi;
    private readonly ICachedCurrencyApi _cachedCurrencyApi;

    /// <inheritdoc cref="CurrencyService" />
    /// <param name="logger">Логгер</param>
    /// <param name="dbContext">Контекст базы данных</param>
    /// <param name="cacheSettings">Настройки приложения</param>
    /// <param name="conversionQueue">Очередь задач пересчета курса валют относительно новой</param>
    /// <param name="currencyApi">Сервис внешнего API</param>
    public CurrencyService(ILogger<CurrencyService> logger, AppDbContext dbContext,
        IOptionsSnapshot<CurrencyCacheSettings> cacheSettings,
        IInternalQueue<CurrencyConversionTask> conversionQueue, ICurrencyApi currencyApi, ICachedCurrencyApi cachedCurrencyApi)
    {
        _logger = logger;
        _dbContext = dbContext;
        _cacheSettings = cacheSettings.Value;
        _conversionQueue = conversionQueue;
        _currencyApi = currencyApi;
        _cachedCurrencyApi = cachedCurrencyApi;
    }

    /// <inheritdoc />
    public async Task<CurrencyDTO> GetCurrency(CurrencyType currencyType, CancellationToken cancellationToken)
    {
        var currency = await _cachedCurrencyApi.GetCurrentCurrencyAsync(currencyType, cancellationToken);

        var dto = ParseDto(currency);

        return dto;
    }

    /// <inheritdoc />
    public async Task<CurrencyDTO> GetCurrencyOnDate(CurrencyType currencyType, DateOnly date, CancellationToken cancellationToken)
    {
        var currency = await _cachedCurrencyApi.GetCurrencyOnDateAsync(currencyType, date, cancellationToken);

        var dto = ParseDto(currency);

        return dto;
    }
    
    /// <summary>
    ///     Маппинг курса валюты из кеша в DTO
    /// </summary>
    /// <param name="currency">Курс валюты из кеша</param>
    /// <returns>Объект <see cref="CurrencyDTO" />, содержащий курс валюты</returns>
    private static CurrencyDTO ParseDto(Currency currency)
    {
        var couldParse = Enum.TryParse(currency.Code.ToString(), true, out CurrencyTypeDTO currencyType);
        if (!couldParse
            && !Enum.IsDefined(typeof(CurrencyTypeDTO), currencyType))
            throw new ArgumentException($"Тип валюты{currency.Code} не поддерживается");

        var parsedDto = new CurrencyDTO
        {
            CurrencyType = currencyType,
            Value = currency.Value.ToString(CultureInfo.InvariantCulture)
        };

        return parsedDto;
    }

    /// <inheritdoc />
    public async Task<Settings> GetCurrencySettings(CancellationToken cancellationToken)
    {
        var settings = new Settings
        {
            BaseCurrency = _cacheSettings.BaseCurrency.ToString(),
            NewRequestsAvailable = await _currencyApi.IsNewRequestsAvailable(cancellationToken)
        };

        return settings;
    }

    /// <inheritdoc />
    public async Task<bool> IsExternalApiHealth(CancellationToken cancellationToken)
    {
        try
        {
            await _currencyApi.IsNewRequestsAvailable(cancellationToken);
            return true;
        }
        catch
        {
            _logger.LogError("CurrencyApi недоступен");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<Guid> CurrencyConversion(CurrencyType newBaseCurrency)
    {
        CurrencyConversionTask conversionTask = new()
        {
            Status = CurrencyConversionStatus.Created,
            NewBaseCurrency = newBaseCurrency,
            StartTime = DateTimeOffset.UtcNow
        };

        // Сохранение в БД задачи CacheTask на выполнение пересчета
        await _dbContext.CurrencyConversionTasks.AddAsync(conversionTask);
        await _dbContext.SaveChangesAsync();
        

        // TODO Отправление задачи во внутреннюю очередь

        Task.Run( () => _conversionQueue.Enqueue(conversionTask));

        _logger.LogInformation("Создана задача пересчета курса валют относительно новой: {NewBaseCurrency}",
            newBaseCurrency);
        
        // Возвращает из метода идентификатор созданной задачи
        return conversionTask.Id;
    }
}