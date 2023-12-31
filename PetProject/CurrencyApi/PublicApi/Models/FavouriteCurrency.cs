﻿using System.ComponentModel.DataAnnotations;
using Fuse8_ByteMinds.SummerSchool.PublicApi.Attributes;
using Fuse8_ByteMinds.SummerSchool.PublicApi.Dtos;
using Fuse8_ByteMinds.SummerSchool.PublicApi.Services.Mapper;

namespace Fuse8_ByteMinds.SummerSchool.PublicApi.Models;

/// <summary>
/// Избранная валюта
/// </summary>
public class FavouriteCurrency : IMapFrom<FavouriteCurrencyDto>, IMapTo<FavouriteCurrencyDto>
{
    
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
    [CurrencyCode]
    public required string Currency { get; set; }
    
    /// <summary>
    /// Базовая валюта
    /// </summary>
    [CurrencyCode]
    public required string BaseCurrency { get; set; }
}