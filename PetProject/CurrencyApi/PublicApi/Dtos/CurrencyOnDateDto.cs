using System.Text.Json.Serialization;
using Fuse8_ByteMinds.SummerSchool.PublicApi.Models;

namespace Fuse8_ByteMinds.SummerSchool.PublicApi.Dtos;

/// <summary>
///     DTO валюты с указанием даты курса
/// </summary>
public class CurrencyOnDateDto : Currency
{
    /// <summary>
    ///     Дата курса валюты
    /// </summary>
    [JsonPropertyName("date")]
    public DateOnly Date { get; set; }
}