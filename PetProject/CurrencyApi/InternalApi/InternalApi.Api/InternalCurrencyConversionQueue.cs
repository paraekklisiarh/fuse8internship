using InternalApi.Entities;

namespace InternalApi;

/// <summary>
///     Внутренняя очередь
/// </summary>
/// <typeparam name="T">Тип задачи во внутренней очереди</typeparam>
public interface IInternalQueue<T>
{
    /// <summary>
    ///     Добавить элемент в очередь
    /// </summary>
    /// <param name="item">Элемент очереди <see cref="T" /></param>
    void Enqueue(T item);

    /// <summary>
    ///     Получить из очереди первый элемент
    /// </summary>
    /// <returns>Элемент очереди <see cref="T" />, если существует. Иначе null.</returns>
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
    /// <returns>Задача по пересчету курса валют, если существует. Иначе null.</returns>
    public CurrencyConversionTask? Dequeue()
    {
        _queue.TryDequeue(out var item);
        return item;
    }
}