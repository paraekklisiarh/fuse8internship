using System.Text.Json.Serialization;

namespace InternalApi.Entities;

/// <summary>
/// Курс валюты
/// </summary>
public record Currency
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
public record CurrenciesOnDate
{
    /// <summary>
    /// Дата обновления данных
    /// </summary>
    public DateTime LastUpdatedAt { get; set; }
    
    /// <summary>
    /// Список курсов валют
    /// </summary>
    [JsonPropertyName("value")]
    public required Currency[] Currencies { get; set; }
}