namespace InternalApi.Entities;

/// <summary>
///     Задача пересчета кеша относительно новой базовой валюты
/// </summary>
public class CurrencyConversionTask
{
    /// <summary>
    ///     Идентификатор задачи
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     Статус задачи
    /// </summary>
    public CurrencyConversionStatus Status { get; set; }

    /// <summary>
    ///     Новая базовая валюта
    /// </summary>
    public required CurrencyType NewBaseCurrency { get; set; }

    /// <summary>
    ///     Время создания задачи
    /// </summary>
    public required DateTimeOffset StartTime { get; set; }

    /// <summary>
    ///     Время окончания выполнения задачи
    /// </summary>
    public DateTimeOffset? EndTime { get; set; }
}

/// <summary>
///     Статусы задачи пересчета кеша
/// </summary>
public enum CurrencyConversionStatus
{
    Created = 0,
    Processed = 1,
    Success = 2,
    Error = 3,
    Canceled = 4
}