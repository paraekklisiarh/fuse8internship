using System.Text.Json.Serialization;
using InternalApi.Entities;

namespace InternalApi.Dtos;

/// <summary>
///     Получаемый из внешнего API объект
/// </summary>
public class CurrencyApiDto
{
    /// <summary>
    /// Мета-данные запроса
    /// </summary>
    [JsonPropertyName("meta")]
    public MetaData? Meta { get; set; }
    
    /// <summary>
    /// Содержимое запроса - словарь код_валюты : <see cref="Currency"/>
    /// </summary>
    [JsonPropertyName("data")]
    public Dictionary<string, Currency>? Data { get; set; }
}

/// <summary>
///     Метадата получаемого из внешнего API объекта <see cref="CurrencyApiDto" />
/// </summary>
public class MetaData
{
    /// <summary>
    /// Дата последнего обновления курса валюты
    /// </summary>
    [JsonPropertyName("last_updated_at")]
    public DateTime LastUpdatedAt { get; set; }
}