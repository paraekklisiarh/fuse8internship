using System.Collections.Concurrent;

namespace InternalApi.Services;

/// <summary>
/// Сервис, предоставляющий глобальный словарь блокировок для обновления кеша
/// </summary>
public class RenewalDatesDictionary
{
    
    /// <summary>
    /// Глобальный словарь блокировок для обновления кеша 
    /// </summary>
    public readonly ConcurrentDictionary<DateOnly, SemaphoreSlim> RenewalDatesLockDictionary = new();

}