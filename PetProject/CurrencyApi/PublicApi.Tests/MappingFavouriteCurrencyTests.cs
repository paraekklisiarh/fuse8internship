using AutoMapper;
using Fuse8_ByteMinds.SummerSchool.PublicApi.Dtos;
using Fuse8_ByteMinds.SummerSchool.PublicApi.Models;
using Fuse8_ByteMinds.SummerSchool.PublicApi.Services.Mapper;

namespace PublicApi.Tests;

public class MappingFavouriteCurrencyTests
{
    private readonly IMapper _sut;

    public MappingFavouriteCurrencyTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ApplicationProfile>());
        _sut = config.CreateMapper();
    }

    [Fact]
    public async Task AutoMapper_MapEntityToDto()
    {
        // Arrange
        var name = "RubToEur";
        var currency = "Rub";
        var baseCurrency = "Usd";

        var entity = new FavouriteCurrency { Id = 1, Name = name, Currency = currency, BaseCurrency = baseCurrency };
        // Act

        var actual = _sut.Map<FavouriteCurrencyDto>(entity);

        // Assert
        
        Assert.Equal(name, actual.Name);
        Assert.Equal(currency, actual.Currency);
        Assert.Equal(baseCurrency, actual.BaseCurrency);
    }

    [Fact]
    public async Task AutoMapper_MapDtoToEntity()
    {
        // Arrange
        var name = "RubToEur";
        var currency = "Rub";
        var baseCurrency = "Usd";

        var dto = new FavouriteCurrencyDto
        {
            Name = name,
            Currency = currency,
            BaseCurrency = baseCurrency
        };
        // Act

        var actual = _sut.Map<FavouriteCurrency>(dto);

        // Assert
        
        Assert.Equal(name, actual.Name);
        Assert.Equal(currency, actual.Currency);
        Assert.Equal(baseCurrency, actual.BaseCurrency);
    }
}