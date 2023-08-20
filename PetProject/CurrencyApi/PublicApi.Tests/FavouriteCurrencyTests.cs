using System.Globalization;
using CurrencyApi;
using Fuse8_ByteMinds.SummerSchool.PublicApi;
using Fuse8_ByteMinds.SummerSchool.PublicApi.Models;
using Fuse8_ByteMinds.SummerSchool.PublicApi.Services;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Moq;
using FavouriteCurrency = Fuse8_ByteMinds.SummerSchool.PublicApi.Models.FavouriteCurrency;

namespace PublicApi.Tests;

public class FavouriteCurrencyTests : IDisposable
{
    private readonly IFavouriteCurrencyService _sut;
    private readonly AppDbContext _mockDbContext;

    private CurrencyApiSetting _setting = new()
    {
        DefaultCurrency = "RUB",
        CurrencyRoundCount = 2
    };

    private readonly Mock<IApiSettingsService> _currencyApiSettingsMock = new();
    private readonly Mock<GetCurrency.GetCurrencyClient> _getCurrencyClientMock = new();

    public FavouriteCurrencyTests()
    {
        _currencyApiSettingsMock.Setup(m => m.GetDefaultCurrencyAsync(It.IsAny<CancellationToken>()))
            .Returns(async () => _setting.DefaultCurrency);
        _currencyApiSettingsMock.Setup(m => m.GetCurrencyRoundCountAsync(It.IsAny<CancellationToken>()))
            .Returns(async () => (int)_setting.CurrencyRoundCount!);

        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _mockDbContext = new AppDbContext(dbOptions);
        _sut = new FavouriteCurrencyService(_mockDbContext, _getCurrencyClientMock.Object,
            _currencyApiSettingsMock.Object);
    }

    public void Dispose()
    {
        _mockDbContext.Dispose();
    }

    [Fact]
    public async Task GetFavouriteCurrencyAsync_ReturnExistFavourite()
    {
        // Arrange
        var ct = CancellationToken.None;

        var name = "RubToEur";
        var entity = new FavouriteCurrency(name, "C", "BC");
        _mockDbContext.FavouriteCurrencies.Add(entity);
        await _mockDbContext.SaveChangesAsync(ct);

        // Act
        var actual = await _sut.GetFavouriteCurrencyAsync(name, ct);

        // Assert
        Assert.Equal(entity, actual);
    }

    [Fact]
    public async Task GetFavouriteCurrencyAsync_ReturnException_WhenFavouriteNotExist()
    {
        // Arrange
        var ct = CancellationToken.None;
        var name = "RubToEur";

        // Act and Assert
        await Assert.ThrowsAsync<FavouriteCurrencyNotFoundException>(() => _sut.GetFavouriteCurrencyAsync(name, ct));
    }

    [Fact]
    public async Task GetFavouritesCurrenciesAsync_ReturnFavorites_WhenFavoritesExist()
    {
        // Arrange
        var ct = CancellationToken.None;

        foreach (var num in Enumerable.Range(1, 10))
        {
            var entity = new FavouriteCurrency($"FC_{num}", $"C_{num}", "BC");
            _mockDbContext.FavouriteCurrencies.Add(entity);
        }

        await _mockDbContext.SaveChangesAsync(ct);

        // Act

        var actual = await _sut.GetFavouritesCurrenciesAsync(ct);

        // Assert
        Assert.True(actual.Count() == 10);
    }

    [Fact]
    public async Task GetFavouritesCurrenciesAsync_ReturnZero_WhenFavoritesNotExist()
    {
        // Arrange
        var ct = CancellationToken.None;

        // Act

        var actual = await _sut.GetFavouritesCurrenciesAsync(ct);

        // Assert
        Assert.True(!actual.Any());
    }

    [Fact]
    public async Task AddFavouriteCurrencyAsync_ThrowException_WhenNonUniqueName()
    {
        // Arrange
        var ct = CancellationToken.None;
        var name = "RubToEur";
        var entity = new FavouriteCurrency(name, "C", "BC");
        _mockDbContext.FavouriteCurrencies.Add(entity);
        await _mockDbContext.SaveChangesAsync(ct);

        var newEntity = new FavouriteCurrency(name, "NC", "NBC");

        // Act and Assert
        await Assert.ThrowsAsync<NotUniqueFavouriteCurrencyException>(
            () => _sut.AddFavouriteCurrencyAsync(newEntity, ct));
    }

    [Fact]
    public async Task AddFavouriteCurrencyAsync_ThrowException_WhenNonUniqueCurrencies()
    {
        // Arrange
        var ct = CancellationToken.None;
        var name = "RubToEur";
        var entity = new FavouriteCurrency(name, "C", "BC");
        _mockDbContext.FavouriteCurrencies.Add(entity);
        await _mockDbContext.SaveChangesAsync(ct);

        var nonUniqEntity = new FavouriteCurrency("NewName", "C", "BC");

        var uniqEntity = new FavouriteCurrency("NewName", "NC", "BC");

        // Act and Assert
        await Assert.ThrowsAsync<NotUniqueFavouriteCurrencyException>(() =>
            _sut.AddFavouriteCurrencyAsync(nonUniqEntity, ct));

        // Только сочетание Валюта+Базовая валюта должно быть уникально.
        var exception = await Record.ExceptionAsync(() => _sut.AddFavouriteCurrencyAsync(uniqEntity, ct));
        Assert.Null(exception);
    }

    [Fact]
    public async Task AddFavouriteCurrencyAsync_SuccessfullyAddEntity()
    {
        // Arrange
        var ct = CancellationToken.None;
        var name = "RubToEur";
        var entity = new FavouriteCurrency(name, "C", "BC");

        // Act
        await _sut.AddFavouriteCurrencyAsync(entity, ct);

        // Assert
        Assert.True(_mockDbContext.FavouriteCurrencies.Any());
    }

    [Fact]
    public async Task EditFavouriteCurrencyAsync_ThrowException_WhenNonUniqueCurrencies()
    {
        // Arrange
        var ct = CancellationToken.None;
        var name = "RubToEur";
        var entities = new List<FavouriteCurrency>
        {
            new(name, "C", "BC"),
            new("other", "NC", "NBC"),
            new("otherUnique", "OC", "NBC")
        };
        _mockDbContext.FavouriteCurrencies.AddRange(entities);
        await _mockDbContext.SaveChangesAsync(ct);

        var newName = "edited";
        var editedEntity = new FavouriteCurrency(newName, "NC", "NBC");

        // Act and Assert
        await Assert.ThrowsAsync<NotUniqueFavouriteCurrencyException>(() =>
            _sut.EditFavouriteCurrencyAsync(name, editedEntity, ct));
    }

    [Fact]
    public async Task EditFavouriteCurrencyAsync_ThrowException_WhenNonUniqueName()
    {
        // Arrange
        var ct = CancellationToken.None;
        var name = "RubToEur";
        var entities = new List<FavouriteCurrency>
        {
            new(name, "C", "BC"),
            new("otherUnique", "OC", "NBC")
        };
        _mockDbContext.FavouriteCurrencies.AddRange(entities);
        await _mockDbContext.SaveChangesAsync(ct);

        var newName = "otherUnique";
        var editedEntity = new FavouriteCurrency(newName, "NC", "NBC");

        // Act and Assert
        await Assert.ThrowsAsync<NotUniqueFavouriteCurrencyException>(() =>
            _sut.EditFavouriteCurrencyAsync(name, editedEntity, ct));
    }

    [Fact]
    public async Task EditFavouriteCurrencyAsync_ThrowException_WhenEntityNotExist()
    {
        // Arrange
        var ct = CancellationToken.None;
        var name = "RubToEur";
        var entities = new List<FavouriteCurrency>
        {
            new("other", "NC", "NBC"),
            new("otherUnique", "OC", "NBC")
        };
        _mockDbContext.FavouriteCurrencies.AddRange(entities);
        await _mockDbContext.SaveChangesAsync(ct);

        var newName = "newName";
        var editedEntity = new FavouriteCurrency(newName, "NC", "NBC");

        // Act and Assert
        await Assert.ThrowsAsync<FavouriteCurrencyNotFoundException>(() =>
            _sut.EditFavouriteCurrencyAsync(name, editedEntity, ct));
    }

    [Fact]
    public async Task EditFavouriteCurrencyAsync_SuccessfullyEdit()
    {
        // Arrange
        var ct = CancellationToken.None;
        var name = "RubToEur";
        var entity = new FavouriteCurrency(name, "C", "BC");
        _mockDbContext.FavouriteCurrencies.Add(entity);
        await _mockDbContext.SaveChangesAsync(ct);

        var editedEntity = new FavouriteCurrency("edited", "NC", "NBC");

        // Act
        await _sut.EditFavouriteCurrencyAsync(name, editedEntity, ct);

        var actual = await _mockDbContext.FavouriteCurrencies.FirstOrDefaultAsync(ct);

        // Assert
        Assert.Equal(editedEntity.Name, actual?.Name);
        Assert.Equal(editedEntity.Currency, actual?.Currency);
        Assert.Equal(editedEntity.BaseCurrency, actual?.BaseCurrency);
    }

    [Fact]
    public async Task DeleteFavouriteCurrencyAsync_SuccessfullyDeleteEntity()
    {
        // Arrange
        var ct = CancellationToken.None;
        var name = "RubToEur";
        var entity = new FavouriteCurrency(name, "C", "BC");
        _mockDbContext.FavouriteCurrencies.Add(entity);
        await _mockDbContext.SaveChangesAsync(ct);

        // Act
        await _sut.DeleteFavouriteCurrencyAsync(name, ct);

        // Assert
        Assert.False(_mockDbContext.FavouriteCurrencies.Any());
    }

    [Fact]
    public async Task DeleteFavouriteCurrencyAsync_ThrowException_WhenEntityNotExist()
    {
        // Arrange
        var ct = CancellationToken.None;
        var name = "RubToEur";

        // Act and assert

        await Assert.ThrowsAsync<FavouriteCurrencyNotFoundException>(() => _sut.DeleteFavouriteCurrencyAsync(name, ct));
    }

    [Fact]
    public async Task GetFavouriteCurrencyCurrent_ReturnCurrency_AndItIsRounded()
    {
        // Arrange
        var ct = CancellationToken.None;
        var name = "RubToUsd";
        var currency = "RUB";
        decimal value = 100.99999999m;
        var roundCount = 2;

        var entity = new FavouriteCurrency(name, "Rub", "Usd");
        _mockDbContext.FavouriteCurrencies.Add(entity);
        await _mockDbContext.SaveChangesAsync(ct);

        _currencyApiSettingsMock.Setup(s => s.GetCurrencyRoundCountAsync(ct))
            .ReturnsAsync(roundCount);

        var dto = new CurrencyDTO
        {
            CurrencyType = CurrencyTypeDTO.Rub,
            Value = value.ToString(CultureInfo.InvariantCulture)
        };
        var grpcResponse = new AsyncUnaryCall<CurrencyDTO>(Task.FromResult(dto), Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess, () => new Metadata(), () => { });

        _getCurrencyClientMock.Setup(g => g.GetFavouriteCurrencyCurrentAsync(It.IsAny<CurrencyApi.FavouriteCurrency>(),
                It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(grpcResponse);

        // Act

        var actual = await _sut.GetFavouriteCurrencyCurrentAsync(name, ct);

        // Assert

        Assert.Equal(Math.Round(value, roundCount), actual.Value);
        Assert.Equal(currency, actual.Code);
    }
    
    [Fact]
    public async Task GetFavouriteCurrencyOnDate_ReturnCurrency_AndItIsRounded()
    {
        // Arrange
        var ct = CancellationToken.None;
        var name = "RubToUsd";
        var currency = "RUB";
        decimal value = 100.99999999m;
        var roundCount = 2;
        var targetDate = DateOnly.Parse("2000-02-02");

        var entity = new FavouriteCurrency(name, "Rub", "Usd");
        _mockDbContext.FavouriteCurrencies.Add(entity);
        await _mockDbContext.SaveChangesAsync(ct);

        _currencyApiSettingsMock.Setup(s => s.GetCurrencyRoundCountAsync(ct))
            .ReturnsAsync(roundCount);

        var dto = new CurrencyDTO
        {
            CurrencyType = CurrencyTypeDTO.Rub,
            Value = value.ToString(CultureInfo.InvariantCulture)
        };
        var grpcResponse = new AsyncUnaryCall<CurrencyDTO>(Task.FromResult(dto), Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess, () => new Metadata(), () => { });

        _getCurrencyClientMock.Setup(g => g.GetFavouriteCurrencyOnDateAsync(It.IsAny<CurrencyApi.FavouriteCurrencyOnDate>(),
                It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(grpcResponse);

        // Act

        var actual = await _sut.GetFavouriteCurrencyOnDateAsync(name, targetDate, ct);

        // Assert

        Assert.Equal(Math.Round(value, roundCount), actual.Value);
        Assert.Equal(currency, actual.Code);
    }
}