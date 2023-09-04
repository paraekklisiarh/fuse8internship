using InternalApi.Entities;

namespace InternalApi;

/// <summary>
///     Внутренняя очередь
/// </summary>
/// <typeparam name="T">Тип задачи во внутренней очереди</typeparam>
public interface IInternalQueue<T>
{
    void Enqueue(T item);

    T? Dequeue();
}

/// <summary>
///     Внутренняя очередь задач по пересчету курса валют относительно новой базовой валюты
/// </summary>
public class InternalCurrencyConversionQueue : IInternalQueue<CurrencyConversionTask>
{
    private readonly Queue<CurrencyConversionTask?> _queue;

    /// <inheritdoc cref="InternalCurrencyConversionQueue" />
    public InternalCurrencyConversionQueue()
    {
        _queue = new Queue<CurrencyConversionTask?>();
    }

    /// <summary>
    ///     Добавить в очередь задачу по пересчету курса валют
    /// </summary>
    /// <param name="item">Задача по пересчету курса валют</param>
    public void Enqueue(CurrencyConversionTask item)
    {
        _queue.Enqueue(item);
    }

    /// <summary>
    ///     Получить из очереди первую задачу по пересчету курса валют
    /// </summary>
    /// <returns>Задача по пересчету курса валют</returns>
    public CurrencyConversionTask? Dequeue()
    {
        _queue.TryDequeue(out var item);
        return item;
    }
}