namespace Fuse8_ByteMinds.SummerSchool.PublicApi.Dtos;

/// <summary>
/// Текущие настройки API
/// </summary>
public class SettingsDto
{
    /// <summary>
    ///     текущий курс валют по умолчанию из конфигурации
    /// </summary>
    public string? DefaultCurrency { get; set; }

    /// <summary>
    ///     базовая валюта, относительно которой считается курс
    /// </summary>
    public string? BaseCurrency { get; set; }

    /// <summary>
    ///     количество использованных запросов, полученное от внешнего API
    /// </summary>
    public bool? NewRequestsAvailable { get; set; }

    /// <summary>
    ///     Количество знаков после запятой, до которого следует округлять значение курса валют
    /// </summary>
    public int? CurrencyRoundCount { get; set; }
}