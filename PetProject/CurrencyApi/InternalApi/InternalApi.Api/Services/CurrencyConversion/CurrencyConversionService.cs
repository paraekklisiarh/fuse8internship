using InternalApi.Configuration;
using InternalApi.Entities;
using InternalApi.Infrastructure.Data.CurrencyContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InternalApi.Services.CurrencyConversion;

/// <summary>
///     Сервис для пересчета хранимых курсов валют относительно новой базовой валюты
/// </summary>
internal interface ICurrencyConversionService
{
    /// <summary>
    ///     Начинает пересчет кеша относительно новой базовой валюты
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <param name="taskId"></param>
    public Task CurrencyConversion(Guid taskId, CancellationToken cancellationToken);
}

/// <inheritdoc cref="ICurrencyConversionService" />
public class CurrencyConversionService : ICurrencyConversionService
{
    private readonly ILogger<CurrencyConversionService> _logger;
    private readonly IConfiguration _configuration;
    private readonly CurrencyCacheSettings _cacheSettings;
    private readonly AppDbContext _dbContext;
    private readonly SemaphoreSlim _conversionSemaphore = new(1, 1);

    /// <inheritdoc cref="CurrencyConversionService" />
    /// <param name="dbContext">Контекст базы данных</param>
    /// <param name="cacheSettings">Текущие настройки кеша</param>
    /// <param name="logger">Логгер</param>
    /// <param name="configuration">Текущая конфигурация приложения</param>
    public CurrencyConversionService(AppDbContext dbContext, IOptionsMonitor<CurrencyCacheSettings> cacheSettings,
        ILogger<CurrencyConversionService> logger, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _cacheSettings = cacheSettings.CurrentValue;
        _logger = logger;
        _configuration = configuration;
    }

    /// <inheritdoc />
    public async Task CurrencyConversion(Guid taskId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Только один поток должен пересчитывать кеш.
        await _conversionSemaphore.WaitAsync(cancellationToken);

        // Получаем задачу из базы данных
        _logger.LogInformation("Получена задача пересчета кеша валют {TaskId}", taskId);
        var task = await _dbContext.CurrencyConversionTasks.FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

        if (task == null) return;

        //// Подготовка к пересчёту курсов валюты
        var oldBaseCurrencyCode = _cacheSettings.BaseCurrency;
        var newBaseCurrencyCode = task.NewBaseCurrency;

        if (oldBaseCurrencyCode == newBaseCurrencyCode)
        {
            task.Status = CurrencyConversionStatus.Canceled;
            task.EndTime = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Базовая валюта задачи {TaskId} уже соответствует текущей", task.Id);

            return;
        }

        task.Status = CurrencyConversionStatus.Processed;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Начата обработка задачи пересчета курса валют: {OldBaseCurrencyCode} -> {NewBaseCurrencyCode}",
            oldBaseCurrencyCode, newBaseCurrencyCode);

        var allDates = await _dbContext.Currencies.AsNoTracking()
            .Select(c => c.RateDate)
            .Distinct()
            .ToListAsync(cancellationToken);

        //// Выполнение задачи

        cancellationToken.ThrowIfCancellationRequested();
        /*
         * Я попробовал перенести вычисления в базу данных, формируя на каждую дату SQL запрос типа:
         * UPDATE cur.currencies SET value = cur.currencies.value * {multiplier.ToString(CultureInfo.InvariantCulture)} WHERE rate_date = '{date}';
         * Однако выяснилось, что NUMERIC хранит знаков после запятой куда больше, чем decimal ( => переполнение стека),
         * а ограничение NUMERIC(precision,scale) фиксирует количество знаков после запятой, что приведёт к потере данных.
         * Поэтому вычисления происходят на стороне приложения.
         */

        // Задача оборачивается в транзакцию, чтобы обеспечить целостность данных в случае возникновения ошибки.

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // Пересчёт курса
                foreach (var date in allDates)
                {
                    var currencies = await _dbContext.Currencies.Where(c => c.RateDate == date)
                        .ToListAsync(cancellationToken);

                    var oldBaseCurrency =
                        currencies.FirstOrDefault(c => c.RateDate == date && c.Code == oldBaseCurrencyCode);
                    if (oldBaseCurrency == null)
                        throw new CurrencyConversionNotFoundException(
                            $"Значение базовой валюты {oldBaseCurrencyCode} не найдено на дату {date}. Кеш повреждён");

                    var newBaseCurrency =
                        currencies.FirstOrDefault(c => c.RateDate == date && c.Code == newBaseCurrencyCode);
                    if (newBaseCurrency == null)
                        throw new CurrencyConversionNotFoundException(
                            $"Значение валюты {newBaseCurrencyCode} не найдено на дату {date}");

                    var multiplier = 1 / newBaseCurrency.Value;

                    foreach (var currency in currencies) currency.Value *= multiplier;
                }

                _logger.LogInformation("Задача {TaskId}: пересчет валют успешно завершен", task.Id);

                // Завершаем транзакцию
                await transaction.CommitAsync(cancellationToken);

                // Меняем статус задачи на выполненный
                task.Status = CurrencyConversionStatus.Success;
                task.EndTime = DateTimeOffset.UtcNow;
                await _dbContext.SaveChangesAsync(cancellationToken);

                // Сохраняется новая базовая валюта
                _configuration["Cache:CurrencyAPICache:BaseCurrency"] = task.NewBaseCurrency.ToString();
                _logger.LogInformation("Задача {TaskId} завершена успешно", task.Id);
            }
            catch (Exception e)
            {
                // Откатываем транзакцию
                await transaction.RollbackAsync(cancellationToken);

                // Устанавливаем статус задачи "Ошибка"
                task.Status = CurrencyConversionStatus.Error;
                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogError(e,
                    "Произошла ошибка при смене базовой валюты {OldBaseCurrencyCode} на {NewBaseCurrencyCode}",
                    oldBaseCurrencyCode, newBaseCurrencyCode);
            }
            finally
            {
                _conversionSemaphore.Release();
            }
        });
    }
}

/// <summary>
///     Выбрасывается в случае, когда не найден курс базовой валюты, относительно которого следует пересчитывать кеш.
///     NB! Целостность БД нарушена
/// </summary>
public class CurrencyConversionNotFoundException : Exception
{
    /// <inheritdoc />
    public CurrencyConversionNotFoundException(string? message) : base(message)
    {
    }
}