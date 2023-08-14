using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.Json;
using InternalApi.Contracts;
using InternalApi.Entities;
using Microsoft.Extensions.Options;
using Nito.AsyncEx;
using TestGrpc;

namespace InternalApi.Services;

/// <summary>
///     Сервис для получения данных из файлового кеша
/// </summary>
public class CurrencyCacheFileService : ICurrencyCacheFileService
{
    private readonly ILogger<CurrencyCacheFileService> _logger;
    private readonly CurrencyCacheSettings _cacheSettings;

    private readonly ICurrencyApi _currencyApi;

    // Коллекция для хранения дат, на которые в настоящий момент обновляются данные из внешнего API
    private readonly ConcurrentDictionary<DateOnly, AsyncLock> _renewalDatesLockDictionary = new();
    private readonly string _cacheFolderPath;

    /// <summary>
    ///     Конструктор класса CurrencyCacheFileService
    /// </summary>
    /// <param name="logger">Логгер</param>
    /// <param name="currencyApi">Сервис для получения данных из внешнего API</param>
    /// <param name="cacheSettings">Настройки файлового кеша</param>
    public CurrencyCacheFileService(ILogger<CurrencyCacheFileService> logger,
        ICurrencyApi currencyApi,
        IOptionsMonitor<CurrencyCacheSettings> cacheSettings
    )
    {
        _logger = logger;
        _currencyApi = currencyApi;
        _cacheSettings = cacheSettings.CurrentValue;
        _cacheFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "_cache", "CurrencyAPI");
    }

    /// <inheritdoc />
    public async Task<Currency> GetEntity(CurrencyType currencyType, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Получение курса валюты из кеша");

        // Попытка найти название Dto в кеше
        var dtoName = await FindEntityOrUpdateCache(cancellationToken);

        var currency = await ReadAsync(dtoName, currencyType, cancellationToken);
        return currency;
    }

    /// <inheritdoc />
    public async Task<Currency> GetEntity(CurrencyType currencyType, DateOnly dateOnly,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Получение курса валюты из кеша");

        // Попытка найти название Dto в кеше
        var dtoName = await FindEntityOrUpdateCache(cancellationToken, dateOnly);

        var currency = await ReadAsync(dtoName, currencyType, cancellationToken);
        return currency;
    }

    /// <summary>
    ///     Получение сущностей из API
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <param name="currencyDate">Дата курса валюты. Если null, то UtcNow"/></param>
    private async Task UpdateEntitiesFromApi(CancellationToken cancellationToken, DateOnly? currencyDate = null)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Актуальный курс валют не найден. Обновляю");
            if (currencyDate is null)
            {
                var updatedDto =
                    await _currencyApi.GetAllCurrentCurrenciesAsync(_cacheSettings.BaseCurrency.ToString(),
                        cancellationToken);

                await SaveDtoAsync(updatedDto, cancellationToken);
            }
            else
            {
                var updatedDto =
                    await _currencyApi.GetAllCurrenciesOnDateAsync(_cacheSettings.BaseCurrency.ToString(),
                        (DateOnly)currencyDate,
                        cancellationToken);

                await SaveDtoAsync(updatedDto.Currencies, cancellationToken,
                    DateOnly.FromDateTime(updatedDto.LastUpdatedAt));
            }

            _logger.LogInformation("Курс валют актуализирован");

            return;
        }

        throw new OperationCanceledException();
    }

    /// <summary>
    ///     Метод для кеширования новых <see cref="Currency" />.
    /// </summary>
    /// <param name="currencies">Список объектов курса валюты</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <param name="date">Дата курса валюты DTO. Если null, то UtcNow.</param>
    private async Task SaveDtoAsync(IEnumerable<Currency> currencies, CancellationToken cancellationToken,
        DateOnly? date = null)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Кеширую курс валют");

            var fileName = date is null
                ? $"{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.CurrencyCache.json"
                : $"{((DateOnly)date).ToDateTime(new TimeOnly()):yyyy-MM-dd-HH-mm-ss}.CurrencyCache.json";

            var serializeToUtf8Bytes = JsonSerializer.SerializeToUtf8Bytes(currencies);

            await using var fileStream = new FileStream(
                Path.Combine(_cacheFolderPath, fileName), FileMode.Create, FileAccess.Write);
            await fileStream.WriteAsync(serializeToUtf8Bytes, cancellationToken);


            _logger.LogInformation("Курс валют кеширован");

            return;
        }
    }

    /// <summary>
    ///     Метод для чтения кешированных сущностей
    /// </summary>
    /// <param name="fileName">Название файла, содержащего сущность</param>
    /// <param name="currencyType">Тип валюты</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Полученный из кеша объект <see cref="CurrencyDTO" /></returns>
    private async Task<Currency> ReadAsync(string fileName, CurrencyType currencyType,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await using var fileStream =
                new FileStream(Path.Combine(_cacheFolderPath, fileName), FileMode.Open, FileAccess.Read);
            using var streamReader = new StreamReader(fileStream, Encoding.UTF8);

            var serializedDto = await streamReader.ReadToEndAsync(cancellationToken);

            var currencies = JsonSerializer.Deserialize<Currency[]>(serializedDto);
            var currency =
                (currencies ?? throw new IOException($"Произошла ошибка при получении {fileName} из кеша"))
                .FirstOrDefault(c => c.Code == currencyType.ToString().ToUpper());

            return currency ?? throw new InvalidOperationException($"Файл кеша {fileName} поврежден");
        }

        throw new OperationCanceledException(cancellationToken);
    }

    /// <summary>
    ///     Поиск кешированных DTO. Если не найдены - обновление кеша и поиск.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <param name="currencyDate">Дата курса валюты. Если не указана - поиск валидного кеша на UtcNow.</param>
    /// <exception cref="IOException">Выбрасывается при неудачной попытке обновления кеша</exception>
    /// <returns>Название файла.</returns>
    private async Task<string> FindEntityOrUpdateCache(CancellationToken cancellationToken,
        DateOnly? currencyDate = null)
    {
        // Метод возвращает название файла, который далее нужно прочитать, найти сущность и вернуть из сервиса.
        // При неудачном поиске метод обновляет кеш
        // Необязательный параметр "дата" призван объединить дублирующийся метод: если null, то UtcNow

        while (!cancellationToken.IsCancellationRequested)
        {
            // Название искомого файла
            var fileName = FindName(cancellationToken, currencyDate);

            if (fileName is not null) return fileName;

            // Если подходящей сущности в кеше нет - обновить кеш
            await Update();
            if (fileName is not null) return fileName;

            // Если файл не найден после обновления - что-то явно пошло не так.
            throw new IOException("Обновление кеша неуспешно");

            // Вложенный метод: обновление данных
            async Task Update()
            {
                // Дата, на которую обновляется кеш
                var updatingDate = currencyDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

                // Если сейчас не обновляется кеш на искомую дату, обновить.
                if (!_renewalDatesLockDictionary.ContainsKey(updatingDate))
                {
                    await UpdateEntitiesFromApi(cancellationToken, currencyDate);
                    fileName = FindName(cancellationToken, currencyDate);
                    return;
                }

                // Если сейчас обновляется кеш на указанную дату, то следует дождаться обновления.
                var asyncLock = _renewalDatesLockDictionary.GetOrAdd(updatingDate, new AsyncLock());
                try
                {
                    using (await asyncLock.LockAsync(cancellationToken))
                    {
                        // За время ожидания могло произойти обновление данных из API.
                        // Повторный поиск.
                        fileName = FindName(cancellationToken, currencyDate);
                        if (fileName is not null) return;

                        await UpdateEntitiesFromApi(cancellationToken, currencyDate);
                        fileName = FindName(cancellationToken, currencyDate);
                    }
                }
                finally
                {
                    _renewalDatesLockDictionary.TryRemove(updatingDate, out _);
                }
            }
        }

        throw new OperationCanceledException(cancellationToken);
    }

    /// <summary>
    ///     Поиск названия файла с сущностью указанного типа валюты в кеше
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <param name="currencyDate">Дата, на которую нужен курс валюты. По умолчанию: UtcNow</param>
    /// <returns>Название файла, если существует. Null - если подходящего не найдено.</returns>
    private string? FindName(CancellationToken cancellationToken,
        DateOnly? currencyDate = null)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            // Не существует директория - не существует кеша.
            if (!Directory.Exists(_cacheFolderPath))
            {
                Directory.CreateDirectory(_cacheFolderPath);
                return null;
            }

            var minTime = DateTime.UtcNow.AddHours(-_cacheSettings.CacheExpirationHours);

            var directoryInfo = new DirectoryInfo(_cacheFolderPath);

            // Получение файлов, соответствующих типу валюты и созданных в указанный промежуток времени.
            // Если указана дата, то получить файлы на указанную дату. Если не указана - сегодняшняя.
            // Выбор файла с максимальной датой
            var files = currencyDate is null
                ? directoryInfo.GetFiles("*.json")
                    .Where(file => DateTime.TryParseExact(
                                       file.Name.Substring(0, 19),
                                       "yyyy-MM-dd-HH-mm-ss",
                                       null,
                                       DateTimeStyles.AssumeUniversal,
                                       out var result)
                                   && result >= minTime)
                    .ToList()
                : directoryInfo.GetFiles("*.json")
                    .Where(file => DateOnly.TryParseExact(
                                       file.Name.Substring(0, 10),
                                       "yyyy-MM-dd",
                                       null,
                                       DateTimeStyles.None,
                                       out var result)
                                   && result == currencyDate)
                    .ToList();

            var maxDateFile = files.MaxBy(file => DateTime.ParseExact(
                file.Name.Substring(0, 19),
                "yyyy-MM-dd-HH-mm-ss",
                null,
                DateTimeStyles.AssumeUniversal
            ));

            return maxDateFile?.Name;
        }

        throw new OperationCanceledException();
    }
}