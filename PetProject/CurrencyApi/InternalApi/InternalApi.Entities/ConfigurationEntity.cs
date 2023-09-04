using System.ComponentModel.DataAnnotations;

namespace InternalApi.Entities;

/// <summary>
///     Элемент конфигурации
/// </summary>
public class ConfigurationEntity
{
    /// <summary>
    ///     Название параметра конфигурации
    /// </summary>
    [Key]
    public required string Key { get; set; }

    /// <summary>
    ///     Значение параметра конфигурации
    /// </summary>
    public required string Value { get; set; }
}