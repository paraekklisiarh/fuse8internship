using System.Text.Json.Serialization;
using Fuse8_ByteMinds.SummerSchool.PublicApi.Attributes;

namespace Fuse8_ByteMinds.SummerSchool.PublicApi.Models;

/// <summary>
///     Валюта
/// </summary>
public class Currency
{
    /// <summary>
    ///     Код валюты
    /// </summary>
    [JsonPropertyName("code")]
    [CurrencyCode]
    public required string Code { get; set; }

    /// <summary>
    ///     Курс валюты
    /// </summary>
    [JsonPropertyName("value")]
    public decimal Value { get; set; }
}