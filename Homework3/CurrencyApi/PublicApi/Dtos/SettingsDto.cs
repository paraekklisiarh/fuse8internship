namespace Fuse8_ByteMinds.SummerSchool.PublicApi.Dtos;

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
    ///     общее количество доступных запросов, полученное от внешнего API
    /// </summary>
    public int RequestLimit { get; set; }

    /// <summary>
    ///     количество использованных запросов, полученное от внешнего API
    /// </summary>
    public int RequestCount { get; set; }

    /// <summary>
    ///     Количество знаков после запятой, до которого следует округлять значение курса валют
    /// </summary>
    public int CurrencyRoundCount { get; set; }
}