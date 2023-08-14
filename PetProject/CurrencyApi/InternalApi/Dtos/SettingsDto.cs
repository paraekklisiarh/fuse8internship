
namespace InternalApi.Dtos;

/// <summary>
/// Настройки API
/// </summary>
public class SettingsDto
{
    /// <summary>
    ///     базовая валюта, относительно которой считается курс
    /// </summary>
    public string? BaseCurrency { get; set; }

    /// <summary>
    ///     количество использованных запросов, полученное от внешнего API
    /// </summary>
    public bool NewRequestsAvailable { get; set; }

    /// <summary>
    ///     Количество знаков после запятой, до которого следует округлять значение курса валют
    /// </summary>
    public int CurrencyRoundCount { get; set; }
}