namespace Fuse8_ByteMinds.SummerSchool.PublicApi.Models;

/// <summary>
/// Настройки внешнего API
/// </summary>
public class CurrencyApiSettings
{
    /// <summary>
    /// Адрес внешнего API
    /// </summary>
    public required string BaseUrl { get; init; }
    
    /// <summary>
    /// Валюта по умолчанию
    /// </summary>
    public required string DefaultCurrency { get; set; }
    
    /// <summary>
    /// Знак после запятой, до которого следует округлять значение курса
    /// </summary>
    public int CurrencyRoundCount { get; init; }
}