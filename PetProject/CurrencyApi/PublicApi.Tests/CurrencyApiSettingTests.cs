using Fuse8_ByteMinds.SummerSchool.PublicApi;
using Fuse8_ByteMinds.SummerSchool.PublicApi.Models;
using Microsoft.EntityFrameworkCore;

namespace PublicApi.Tests;

public class CurrencyApiSettingTests : IDisposable
{
    private readonly AppDbContext _mockDbContext;

    public CurrencyApiSettingTests()
    {
        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _mockDbContext = new AppDbContext(dbOptions);
    }

    public void Dispose()
    {
        _mockDbContext.Dispose();
    }

    [Fact]
    public async Task CurrencyApiSettingTests_ReturnSettings()
    {
        // Arrange
        var ct = CancellationToken.None;
        var settings = new CurrencyApiSetting
        {
            Id = 0,
            DefaultCurrency = "DC",
            CurrencyRoundCount = 10
        };
        await _mockDbContext.CurrencyApiSettings.AddAsync(settings, ct);
        await _mockDbContext.SaveChangesAsync(ct);

        var sut = new CurrencyApiSettings(_mockDbContext);
        // Act

        var defaultCurrency = await sut.GetDefaultCurrencyAsync(ct);
        var currencyRoundCount = await sut.GetCurrencyRoundCountAsync(ct);

        // Assert
        
        Assert.Equal(settings.DefaultCurrency, defaultCurrency);
        Assert.Equal(settings.CurrencyRoundCount, currencyRoundCount);
    }

    [Fact]
    public async Task CurrencyApiSettingTests_SetSettingsSuccessfully()
    {
        // Arrange
        var ct = CancellationToken.None;
        var settings = new CurrencyApiSetting
        {
            Id = 0,
            DefaultCurrency = "DC",
            CurrencyRoundCount = 10
        };
        await _mockDbContext.CurrencyApiSettings.AddAsync(settings, ct);
        await _mockDbContext.SaveChangesAsync(ct);

        var sut = new CurrencyApiSettings(_mockDbContext);

        // инициализируем ленивые поля, чтобы убедиться: вернутся актуальные значения.
        var baseCount = await sut.GetCurrencyRoundCountAsync(ct);
        var baseCurrency = await sut.GetDefaultCurrencyAsync(ct);

        var newDefaultCurrency = "NDC";
        var newCount = 20;
        // Act

        await sut.SetDefaultCurrencyAsync(newDefaultCurrency, ct);
        await sut.SetCurrencyRoundCountAsync(newCount, ct);
        
        // Assert
        
        Assert.NotNull(baseCurrency);
        Assert.NotEqual(0, baseCount);
        
        Assert.Equal(newCount, await sut.GetCurrencyRoundCountAsync(ct));
        Assert.Equal(newDefaultCurrency, await sut.GetDefaultCurrencyAsync(ct));
    }
    
    [Fact]
    public async Task CurrencyApiSettingTests_WriteSettingsToDb()
    {
        // Arrange
        var ct = CancellationToken.None;
        // var settings = new CurrencyApiSetting
        // {
        //     Id = 0,
        //     DefaultCurrency = "DC",
        //     CurrencyRoundCount = 10
        // };
        // await _mockDbContext.CurrencyApiSettings.AddAsync(settings, ct);
        // await _mockDbContext.SaveChangesAsync(ct);

        var sut = new CurrencyApiSettings(_mockDbContext);

        var newDefaultCurrency = "NDC";
        List<int?> newCounts = new();

        List<int?> actualCounts = new();
        // Act

        await sut.SetDefaultCurrencyAsync(newDefaultCurrency, ct);
        foreach (var newCount in Enumerable.Range(1, 10))
        {
            await sut.SetCurrencyRoundCountAsync(newCount, ct);
            newCounts.Add(newCount);
            actualCounts.Add(_mockDbContext.CurrencyApiSettings.FirstOrDefault().CurrencyRoundCount);
        }
        
        // Assert
        
        Assert.Equal(newCounts, actualCounts);
        Assert.Equal(newDefaultCurrency, _mockDbContext.CurrencyApiSettings.FirstOrDefault().DefaultCurrency);
    }

    [Fact]
    public async Task CurrencyApiSettingTests_ThrowException_WhenSettingsNotExistInDb()
    {
        // Arrange
        var ct = CancellationToken.None;
        var sut = new CurrencyApiSettings(_mockDbContext);
        
        // Act and assert

        await Assert.ThrowsAsync<ApiSettingsAreNotSet>( () => sut.GetDefaultCurrencyAsync(ct) );
        await Assert.ThrowsAsync<ApiSettingsAreNotSet>( () => sut.GetCurrencyRoundCountAsync(ct) );
    }
}