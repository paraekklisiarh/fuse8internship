using System.ComponentModel.DataAnnotations;

namespace Fuse8_ByteMinds.SummerSchool.PublicApi.Models;

/// <summary>
///     Настройки внешнего API
/// </summary>
public class CurrencyApiSetting
{
    /// <summary>
    ///     Идентификатор записи в базе данных
    /// </summary>
    [Key]
    public byte Id { get; set; }

    /// <summary>
    ///     Валюта по умолчанию
    /// </summary>
    [RegularExpression("[A-Z]{3}")]
    public string? DefaultCurrency { get; set; }

    /// <summary>
    ///     Знак после запятой, до которого следует округлять значение курса
    /// </summary>
    [MaxLength(27)]
    public int? CurrencyRoundCount { get; set; }
}
