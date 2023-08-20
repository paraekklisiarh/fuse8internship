using InternalApi.Contracts;
using InternalApi.Dtos;
using InternalApi.Entities;
using InternalApi.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InternalApi.Services;

/// <summary>
///     Сервис кеша для CurrencyApi
/// </summary>
public class CachedCurrencyApi : ICachedCurrencyApi
{
    private readonly ILogger<CachedCurrencyApi> _logger;
    private readonly ICurrencyApi _currencyApi;
    private readonly CurrencyCacheSettings _cacheSettings;
    private readonly AppDbContext _dbContext;

    // Коллекция для хранения дат, на которые в настоящий момент обновляются данные из внешнего API
    private readonly RenewalDatesDictionary _cacheUpdateLock;

    /// <summary>
    ///     Конструктор класса CachedCurrencyApi
    /// </summary>
    /// <param name="logger">Логгер</param>
    /// <param name="currencyApi">Сервис внешнего API</param>
    /// <param name="cacheSettings">Настройки кеша</param>
    /// <param name="dbContext">База данных</param>
    /// <param name="cacheUpdateLock">Глобальный словарь блокировок для обновления кеша</param>
    public CachedCurrencyApi(ILogger<CachedCurrencyApi> logger, ICurrencyApi currencyApi,
        IOptionsMonitor<CurrencyCacheSettings> cacheSettings, AppDbContext dbContext,
        RenewalDatesDictionary cacheUpdateLock)
    {
        _logger = logger;
        _currencyApi = currencyApi;
        _cacheSettings = cacheSettings.CurrentValue;
        _dbContext = dbContext;
        _cacheUpdateLock = cacheUpdateLock;
    }

    /// <inheritdoc />
    /// <exception cref="CacheEntityNotFoundException">
    ///     Выбрасывается, если после обновления кеша не удалось получить сущность
    /// </exception>
    public async Task<Currency> GetCurrentCurrencyAsync(CurrencyType currencyType, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogInformation("Получение из кеша актуального курса валюты {CurrencyType}", currencyType);

        var currency = await GetEntityAsync(currencyType, null, cancellationToken);

        if (currency is not null) return currency;

        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogInformation("Актуальный курс валюты {CurrencyType} не найден. Обновляю", currencyType);

        // update

        await SecureUpdateCacheAsync(currencyType, null, cancellationToken);

        // repeat get
        _logger.LogInformation("Получение из кеша актуального курса валюты {CurrencyType}", currencyType);

        currency = await GetEntityAsync(currencyType, null, cancellationToken);

        if (currency is not null) return currency;

        // if can't get - throw new exception

        throw new CacheEntityNotFoundException("Не удалось получить сущность из обновленного кеша Currency");
    }

    /// <inheritdoc />
    /// <exception cref="CacheEntityNotFoundException">
    ///     Выбрасывается, если после обновления кеша не удалось получить сущность
    /// </exception>
    public async Task<Currency> GetCurrencyOnDateAsync(CurrencyType currencyType, DateOnly date,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation("Получение из кеша курса валюты {CurrencyType} на {RateDate}", currencyType, date);

        var currency = await GetEntityAsync(currencyType, date, cancellationToken);

        if (currency is not null) return currency;

        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogInformation("Курс валюты {CurrencyType} на {RateDate} не найден. Обновляю", currencyType, date);

        // update

        await SecureUpdateCacheAsync(currencyType, date, cancellationToken);

        // repeat get
        _logger.LogInformation("Получение из кеша курса валюты {CurrencyType} на {RateDate}", currencyType, date);

        currency = await GetEntityAsync(currencyType, date, cancellationToken);

        if (currency is not null) return currency;

        // if can't get - throw new exception

        throw new CacheEntityNotFoundException("Не удалось получить сущность из обновленного кеша Currency");
    }

    /// <summary>
    ///     Получение сущности из кеша
    /// </summary>
    /// <param name="currencyType">Тип валюты</param>
    /// <param name="targetDate">Дата курса валюты. Если null, то текущий курс</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>
    ///     Объект <see cref="Currency" />, содержащий курс валюты или null, если в кеше отсутствует актуальный курс
    ///     валюты
    /// </returns>
    internal async Task<Currency?> GetEntityAsync(CurrencyType currencyType, DateOnly? targetDate,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var minimalTime = DateTime.UtcNow.AddHours(-_cacheSettings.CacheExpirationHours);

        var currency = targetDate is null
            ? await _dbContext.Currencies
                .Where(c => c.Code == currencyType && c.RateDate >= minimalTime)
                .OrderByDescending(r => r.RateDate)
                .FirstOrDefaultAsync(cancellationToken)
            : await _dbContext.Currencies
                .Where(c => c.Code == currencyType &&
                            c.RateDate.Date == targetDate.Value.ToDateTime(new TimeOnly()).Date)
                .OrderByDescending(c => c.RateDate)
                .FirstOrDefaultAsync(cancellationToken);

        return currency;
    }

    /// <summary>
    ///     Потокобезопасное обновление кеша
    /// </summary>
    /// <param name="currencyType">Тип валюты</param>
    /// <param name="targetDate">Дата курса валюты. Если null, то текущий.</param>
    /// <param name="cancellationToken">Токен отмены</param>
    internal async Task SecureUpdateCacheAsync(CurrencyType currencyType, DateOnly? targetDate,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        /*await UpdateSemaphore.WaitAsync(cancellationToken);
        try
        {
            // За время ожидания могло произойти обновление данных из API.
            // Повторный поиск.
            if (await GetEntityAsync(currencyType, targetDate, cancellationToken) == null)
                await UpdateCacheAsync(targetDate, cancellationToken);
        }
        finally
        {
            UpdateSemaphore.Release();
        }*/

        // Дата, на которую обновляется кеш
        var updatingDate = targetDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        SemaphoreSlim updateMutex = new(1, 1);
        try
        {
            // Если сейчас не обновляется кеш на искомую дату, обновить.
            if (!_cacheUpdateLock.RenewalDatesLockDictionary.ContainsKey(updatingDate))
            {
                updateMutex = new SemaphoreSlim(1, 1);
                if (_cacheUpdateLock.RenewalDatesLockDictionary.TryAdd(updatingDate, updateMutex))
                {
                    await updateMutex.WaitAsync(cancellationToken);
                    try
                    {
                        await UpdateCacheAsync(targetDate, cancellationToken);
                    }
                    finally
                    {
                        updateMutex.Release();
                    }
                }
            }
            else
            {
                // Если сейчас обновляется кеш на указанную дату, то следует дождаться обновления.
                updateMutex = _cacheUpdateLock.RenewalDatesLockDictionary
                    .GetOrAdd(updatingDate, new SemaphoreSlim(1, 1));
                await updateMutex.WaitAsync(cancellationToken);
                try
                {
                    // За время ожидания могло произойти обновление данных из API.
                    // Повторный поиск.
                    if (await GetEntityAsync(currencyType, targetDate, cancellationToken) == null)
                        await UpdateCacheAsync(targetDate, cancellationToken);
                }
                finally
                {
                    updateMutex.Release();
                }
            }
        }
        finally
        {
            if (updateMutex.CurrentCount == 0)
            {
                _cacheUpdateLock.RenewalDatesLockDictionary.TryRemove(updatingDate, out _);
            }
        }
    }

    /// <summary>
    ///     Обновление данных на указанную дату.
    /// </summary>
    /// <param name="targetDate">Дата искомого курса валюты. Если дата не указана - получить текущие.</param>
    /// <param name="cancellationToken">Токен отмены</param>
    internal async Task UpdateCacheAsync(DateOnly? targetDate, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Prepare
        var baseCurrency = _cacheSettings.BaseCurrency.ToString().ToUpper();

        // Get new entities
        var apiDto = targetDate is null
            ? await _currencyApi.GetAllCurrentCurrenciesAsync(baseCurrency, cancellationToken)
            : await _currencyApi.GetAllCurrenciesOnDateAsync(baseCurrency,
                (DateOnly)targetDate, cancellationToken);

        // Parse new entities
        var entities = ParseEntity(apiDto);

        // Save new entities
        await SaveEntities(entities, cancellationToken);
    }

    /// <summary>
    ///     Парсинг сущностей из DTO, получаемого от внешнего API
    /// </summary>
    /// <param name="apiDto">DTO от внешнего API</param>
    /// <returns></returns>
    internal IEnumerable<Currency> ParseEntity(RootCurrencyApiDto apiDto)
    {
        if (apiDto.Data == null) return new List<Currency>();

        var rateDate = apiDto.Meta!.LastUpdatedAt;
        var currencies = apiDto.Data.Values.ToList()
            .Select(dto =>
                Enum.TryParse(dto.Code, true, out CurrencyType currencyType)
                    ? new Currency { Code = currencyType, Value = dto.Value, RateDate = rateDate }
                    : null)
            .Where(c => c != null);
        return currencies;
    }

    /// <summary>
    ///     Сохранение сущностей в базу данных
    /// </summary>
    /// <param name="currencies">Перечисление сущностей, которые должны быть сохранены</param>
    /// <param name="cancellationToken">Токен отмены</param>
    internal async Task SaveEntities(IEnumerable<Currency> currencies, CancellationToken cancellationToken)
    {
        await _dbContext.Currencies.AddRangeAsync(currencies, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
///     Не удалось найти сущность в кеше
/// </summary>
public class CacheEntityNotFoundException : Exception
{
    /// <summary>
    ///     Конструктор исключения
    /// </summary>
    /// <param name="message">Сообщение исключения</param>
    public CacheEntityNotFoundException(string? message) : base(message)
    {
    }
}