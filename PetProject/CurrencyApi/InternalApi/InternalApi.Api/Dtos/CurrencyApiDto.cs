using System.Text.Json.Serialization;
using InternalApi.Entities;

namespace InternalApi.Dtos;

/// <summary>
///     Получаемый из внешнего API объект
/// </summary>
public class RootCurrencyApiDto
{
    /// <summary>
    /// Мета-данные запроса
    /// </summary>
    [JsonPropertyName("meta")]
    public MetaCurrencyApiDto? Meta { get; set; }
    
    /// <summary>
    /// Содержимое запроса - словарь код_валюты : <see cref="Currency"/>
    /// </summary>
    [JsonPropertyName("data")]
    public Dictionary<string, CurrencyApiDto>? Data { get; set; }
}

/// <summary>
///     Метадата получаемого из внешнего API объекта <see cref="CurrencyApiDto" />
/// </summary>
public class MetaCurrencyApiDto
{
    /// <summary>
    /// Дата последнего обновления курса валюты
    /// </summary>
    [JsonPropertyName("last_updated_at")]
    public DateTime LastUpdatedAt { get; set; }
}

/// <summary>
/// Курс валюты
/// </summary>
public record CurrencyApiDto
{
    /// <summary>
    ///     Код валюты
    /// </summary>
    [JsonPropertyName("code")]
    public required string Code { get; set; }

    /// <summary>
    ///     Значение курса валюты, относительно базовой валюты
    /// </summary>
    [JsonPropertyName("value")]
    public decimal Value { get; set; }
}

/// <summary>
/// Курсы валют на конкретную дату
/// </summary>
public record CurrenciesOnDateDto
{
    /// <summary>
    /// Дата обновления данных
    /// </summary>
    public DateTime LastUpdatedAt { get; set; }
    
    /// <summary>
    /// Список курсов валют
    /// </summary>
    [JsonPropertyName("value")]
    public required CurrencyApiDto[]? Currencies { get; set; }
}