namespace Fuse8_ByteMinds.SummerSchool.PublicApi.Models;

public class SettingsDto
{
    /// <summary>
    ///  текущий курс валют по умолчанию из конфигурации
    /// </summary>
    public string defaultCurrency { get; set; }

    /// <summary>
    ///  базовая валюта, относительно которой считается курс
    /// </summary>
    public string baseCurrency { get; set; }

    /// <summary>
    /// общее количество доступных запросов, полученное от внешнего API
    /// </summary>
    public int requestLimit { get; set; }

    /// <summary>
    /// количество использованных запросов, полученное от внешнего API
    /// </summary>
    public int requestCount { get; set; }

    /// <summary>
    /// Количество знаков после запятой, до которого следует округлять значение курса валют
    /// </summary>
    public int currencyRoundCount { get; set; }
}