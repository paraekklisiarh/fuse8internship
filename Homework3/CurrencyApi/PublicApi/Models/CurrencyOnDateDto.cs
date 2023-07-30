namespace Fuse8_ByteMinds.SummerSchool.PublicApi.Models;

/// <summary>
/// DTO валюты с указанием даты курса
/// </summary>
public class CurrencyOnDateDto : Currency
{
    /// <summary>
    /// Дата курса валюты
    /// </summary>
    public string date { get; set; }
}
