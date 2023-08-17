namespace InternalApi.Entities;

/// <summary>
///     Настройки внешнего API
/// </summary>
public class CurrencyApiSettings
{
    /// <summary>
    ///     Адрес внешнего API
    /// </summary>
    public required string BaseUrl { get; init; }

    /// <summary>
    ///     Ключ внешнего API
    /// </summary>
    public required string ApiKey { get; init; }
}