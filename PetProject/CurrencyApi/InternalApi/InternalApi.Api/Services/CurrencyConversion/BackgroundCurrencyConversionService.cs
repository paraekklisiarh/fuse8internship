using InternalApi.Entities;
using InternalApi.Infrastructure.Data.CurrencyContext;

namespace InternalApi.Services.CurrencyConversion;

/// <summary>
///     Фоновый сервис очереди пересчета курса валюты
/// </summary>
public class BackgroundCurrencyConversionService : BackgroundService
{
    private readonly ILogger<BackgroundCurrencyConversionService> _logger;
    private readonly IInternalQueue<CurrencyConversionTask> _conversionQueue;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHostApplicationLifetime _lifetime;

    /// <inheritdoc cref="BackgroundCurrencyConversionService" />
    public BackgroundCurrencyConversionService(
        IInternalQueue<CurrencyConversionTask> conversionQueue,
        IServiceProvider serviceProvider,
        ILogger<BackgroundCurrencyConversionService> logger, IHostApplicationLifetime lifetime)
    {
        _conversionQueue = conversionQueue;
        _serviceProvider = serviceProvider;
        _logger = logger;
        this._lifetime = lifetime;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (!await WaitForAppStartup(_lifetime, cancellationToken))
            return;
        
        _logger.LogInformation("Фоновая задача пересчета кеша инициализируется");
        await Prepare(cancellationToken);
            
        _logger.LogInformation("Фоновая задача пересчета кеша активна");
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await BackgroundProcessing(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Фоновый процесс пересчета курса валют остановлен");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Произошла непредвиденная ошибка при работе сервиса пересчета кеша");
        }
    }

    private async Task BackgroundProcessing(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var currencyConversionTask = _conversionQueue.Dequeue();

        if (currencyConversionTask == null) return;

        var conversionService = _serviceProvider.CreateScope().ServiceProvider.GetService<ICurrencyConversionService>();

        try
        {
            _logger.LogInformation("Получена задача пересчета кеша {ID}", currencyConversionTask.Id);

            await conversionService!.CurrencyConversion(currencyConversionTask.Id, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Произошла ошибка при выполнении задачи {ID}", currencyConversionTask.Id);
        }
    }

    private async Task Prepare(CancellationToken cancellationToken)
    {
        await using var dbContext =
            _serviceProvider.CreateAsyncScope().ServiceProvider.GetRequiredService<AppDbContext>();

        var oldTasks = dbContext.CurrencyConversionTasks
            .Where(t =>
                t.Status == CurrencyConversionStatus.Created ||
                t.Status == CurrencyConversionStatus.Processed)
            .ToList();

        if (oldTasks.Any())
        {
            var processedTask = oldTasks.MaxBy(t => t.StartTime);
            _logger.LogInformation("Обнаружена невыполненная задача пересчета кеша {Id}", processedTask!.Id);

            foreach (var task in oldTasks.Where(t => t.Id != processedTask.Id))
            {
                task.Status = CurrencyConversionStatus.Canceled;
                task.EndTime = DateTimeOffset.UtcNow;
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            _conversionQueue.Enqueue(processedTask);
        }
    }

    /// <inheritdoc />
    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Фоновый процесс пересчета курса валют остановлен");

        await base.StopAsync(stoppingToken);
    }

    private static async Task<bool> WaitForAppStartup(IHostApplicationLifetime lifetime, CancellationToken stoppingToken)
    {
        // Создаём TaskCompletionSource для ApplicationStarted
        var startedSource = new TaskCompletionSource();
        await using var reg1 = lifetime.ApplicationStarted.Register(() => startedSource.SetResult());
 
        // Создаём TaskCompletionSource для stoppingToken
        var cancelledSource = new TaskCompletionSource();
        await using var reg2 = stoppingToken.Register(() => cancelledSource.SetResult());
 
        // Ожидаем любое из событий запуска или запроса на остановку
        var completedTask = await Task.WhenAny(startedSource.Task, cancelledSource.Task).ConfigureAwait(false);
 
        // Если завершилась задача ApplicationStarted, возвращаем true, иначе false
        return completedTask == startedSource.Task;
    }

}