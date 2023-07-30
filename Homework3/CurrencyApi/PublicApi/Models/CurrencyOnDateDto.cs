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

/// <summary>
/// 
/// </summary>
public class CurrencyApiDto
{
    public MetaData meta { get; set; }
    public Dictionary<string, Currency> data { get; set; }
}

/// <summary>
/// 
/// </summary>
public class MetaData
{
    public DateTime last_updated_at { get; set; }
}