using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Fuse8_ByteMinds.SummerSchool.PublicApi.Models;

/// <summary>
/// Избранная валюта
/// </summary>
public class FavouriteCurrency
{
    /// <summary>
    /// Класс FavouriteCurrency представляет избранную валюту
    /// </summary>
    /// <param name="name">Имя избранной валюты.</param>
    /// <param name="currency">Код валюты.</param>
    /// <param name="baseCurrency">Код базовой валюты.</param>
    [SetsRequiredMembers]
    public FavouriteCurrency(string name, string currency, string baseCurrency)
    {
        Name = name;
        Currency = currency;
        BaseCurrency = baseCurrency;
    }
    
    /// <summary>
    /// Идентификатор элемента избранного
    /// </summary>
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// Название элемента избранного
    /// </summary>
    public required string Name { get; set; }
    
    /// <summary>
    /// Тип валюты
    /// </summary>
    [RegularExpression("[A-Z]{3}")]
    public required string Currency { get; set; }
    
    /// <summary>
    /// Базовая валюта
    /// </summary>
    [RegularExpression("[A-Z]{3}")]
    public required string BaseCurrency { get; set; }
}