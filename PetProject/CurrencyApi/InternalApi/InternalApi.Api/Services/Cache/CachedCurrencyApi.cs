using InternalApi.Configuration;
using InternalApi.Contracts;
using InternalApi.Dtos;
using InternalApi.Entities;
using InternalApi.Infrastructure.Data.CurrencyContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace InternalApi.Services.Cache;

/// <summary>
///     Сервис кеша для CurrencyApi
/// </summary>
public class CachedCurrencyApi : ICachedCurrencyApi
{
    private readonly IMemoryCache _memoryCache;
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
    /// <param name="memoryCache">Кеш в памяти приложения</param>
    public CachedCurrencyApi(ILogger<CachedCurrencyApi> logger, ICurrencyApi currencyApi,
        IOptionsMonitor<CurrencyCacheSettings> cacheSettings, AppDbContext dbContext,
        RenewalDatesDictionary cacheUpdateLock, IMemoryCache memoryCache)
    {
        _logger = logger;
        _currencyApi = currencyApi;
        _cacheSettings = cacheSettings.CurrentValue;
        _dbContext = dbContext;
        _cacheUpdateLock = cacheUpdateLock;
        _memoryCache = memoryCache;
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

        var memoryCacheKey = currencyType + targetDate.ToString();
        if (_memoryCache.TryGetValue(memoryCacheKey, out Currency? currency))
        {
            _logger.LogInformation("Из кеша в памяти извлечён {Key}", memoryCacheKey);
            return currency;
        }

        var minimalTime = DateTimeOffset.UtcNow.AddHours(-_cacheSettings.CacheExpirationHours);
        
        currency = targetDate is null
            ? await _dbContext.Currencies
                .Where(c => c.Code == currencyType && c.RateDate >= minimalTime)
                .OrderByDescending(r => r.RateDate)
                .FirstOrDefaultAsync(cancellationToken)
            : await _dbContext.Currencies
                .Where(c => c.Code == currencyType &&
                            DateOnly.FromDateTime(c.RateDate.Date) == targetDate)
                .OrderByDescending(c => c.RateDate)
                .FirstOrDefaultAsync(cancellationToken);

        var cacheExpiryOptions = new MemoryCacheEntryOptions
        {
            //ToDo вынести в конфиг кеша
            AbsoluteExpiration = DateTime.Now.AddMinutes(5),
            Priority = CacheItemPriority.High,
            SlidingExpiration = TimeSpan.FromMinutes(2)
        };

        if (currency != null) _memoryCache.Set(memoryCacheKey, currency, cacheExpiryOptions);

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

        // Проверка: выполняется ли операция пересчета кеша?
        if (await _dbContext.CurrencyConversionTasks.AnyAsync(
                t =>
                    t.Status == CurrencyConversionStatus.Processed || t.Status == CurrencyConversionStatus.Created,
                cancellationToken))
        {
            await Task.Delay(10000, cancellationToken);
            if (await _dbContext.CurrencyConversionTasks.AnyAsync(
                    t =>
                        t.Status == CurrencyConversionStatus.Processed || t.Status == CurrencyConversionStatus.Created,
                    cancellationToken))
                throw new CacheUpdateTimeoutException("Обновление кеша не завершено в течение 10 секунд");
        }

        /*
         * Я попытался создать потокобезопасное обновление кеша: только один поток должен пытаться получить кеш от удаленного API потому,
         * что токены - конечный ресурс и следует их оптимизировать как можно сильнее.
         *
         * Я не успел довести до ума: не реализовано очищение словаря. В тестах вроде бы ко внешнему API обращается один раз.
         */

        // Дата, на которую обновляется кеш
        var updatingDate = targetDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        
        // Если сейчас не обновляется кеш на искомую дату, обновить.
        if (!_cacheUpdateLock.RenewalDatesLockDictionary.TryGetValue(updatingDate, out var updatingMutex))
        {
            var updateMutex = new SemaphoreSlim(1, 1);
            if (_cacheUpdateLock.RenewalDatesLockDictionary.TryAdd(updatingDate, updateMutex))
            {
                await updateMutex.WaitAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
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
            await updatingMutex.WaitAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                // За время ожидания могло произойти обновление данных из API.
                // Повторный поиск.
                if (await GetEntityAsync(currencyType, targetDate, cancellationToken) == null)
                    await UpdateCacheAsync(targetDate, cancellationToken);
            }
            finally
            {
                updatingMutex.Release();
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
        await SaveEntities(entities);
    }

    /// <summary>
    ///     Парсинг сущностей из DTO, получаемого от внешнего API
    /// </summary>
    /// <param name="apiDto">DTO от внешнего API</param>
    /// <returns></returns>
    internal IEnumerable<Currency?> ParseEntity(RootCurrencyApiDto apiDto)
    {
        if (apiDto.Data == null) return new List<Currency>();

        var rateDate = apiDto.Meta!.LastUpdatedAt;
        var currencies = apiDto.Data.Values.ToList()
            .Select(dto =>
            {
                var couldParse = Enum.TryParse(dto.Code, true, out CurrencyType currencyType);
                return couldParse && Enum.IsDefined(typeof(CurrencyType), currencyType)
                    ? new Currency { Code = currencyType, Value = dto.Value, RateDate = rateDate }
                    : null;
            })
            .Where(c => c != null);
        return currencies;
    }

    /// <summary>
    ///     Сохранение сущностей в базу данных
    /// </summary>
    /// <param name="enumerable">Перечисление сущностей, которые должны быть сохранены</param>
    /// <remarks>
    ///     Не передаю в метод CancellationToken потому, что уже полученные со внешнего API данные следует сохранить,
    ///     чтобы не потерять токен впустую
    /// </remarks>
    internal async Task SaveEntities(IEnumerable<Currency?> enumerable)
    {
        var currencies = enumerable.Where(c => c != null).Cast<Currency>().ToList();
        await _dbContext.Currencies.AddRangeAsync(currencies);
        await _dbContext.SaveChangesAsync();
    }
}

/// <summary>
///     Представляет ошибку, когда не удалось найти сущность в кеше
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

/// <summary>
///     Представляет ошибку, когда не удалось дождаться обновления кеша
/// </summary>
public class CacheUpdateTimeoutException : Exception
{
    /// <summary>
    ///     Инициализирует новое исключение <see cref="CacheUpdateTimeoutException" />
    /// </summary>
    /// <param name="message">Сообщение исключения</param>
    public CacheUpdateTimeoutException(string? message) : base(message)
    {
    }
}