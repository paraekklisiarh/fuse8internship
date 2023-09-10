using Fuse8_ByteMinds.SummerSchool.PublicApi.Attributes;
using Fuse8_ByteMinds.SummerSchool.PublicApi.Models;
using Fuse8_ByteMinds.SummerSchool.PublicApi.Services.Mapper;

namespace Fuse8_ByteMinds.SummerSchool.PublicApi.Dtos;

/// <summary>
///     Избранная валюта
/// </summary>
public class FavouriteCurrencyDto : IMapFrom<FavouriteCurrency>, IMapTo<FavouriteCurrency>
{
    /// <summary>
    ///     Название элемента избранного
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    ///     Тип валюты
    /// </summary>
    [CurrencyCode]
    public required string Currency { get; set; }

    /// <summary>
    ///     Базовая валюта
    /// </summary>
    [CurrencyCode]
    public required string BaseCurrency { get; set; }
}