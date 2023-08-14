using TestGrpc;

namespace InternalApi.Entities;

/// <summary>
///     Настройки кеша для внешнего API
/// </summary>
public class CurrencyCacheSettings
{
    /// <summary>
    ///     Время жизни кеша
    /// </summary>
    public required int CacheExpirationHours { get; set; }

    /// <summary>
    ///     Базовая валюта, относительно которой считается курс валюты
    /// </summary>
    public required CurrencyType BaseCurrency { get; init; }
}