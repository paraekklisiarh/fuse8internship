using Fuse8_ByteMinds.SummerSchool.PublicApi;
using Fuse8_ByteMinds.SummerSchool.PublicApi.Models;
using Fuse8_ByteMinds.SummerSchool.PublicApi.Services;
using Microsoft.EntityFrameworkCore;

namespace PublicApi.Tests;

public class ApiSettingsServiceTests : IDisposable
{
    private readonly AppDbContext _mockDbContext;
    private readonly IApiSettingsService  _sut;
    
    public ApiSettingsServiceTests()
    {
        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _mockDbContext = new AppDbContext(dbOptions);

        _sut = new ApiSettingsService(_mockDbContext);
    }

    public void Dispose()
    {
        _mockDbContext.Dispose();
    }

    [Fact]
    public async Task DefaultCurrencySetAsync_ThrowException_WhenNotValidCurrencyCode()
    {
        // Arrange
        var ct = CancellationToken.None;
        var code = "Абракадабра";

        // Act and assert
        await Assert.ThrowsAsync<CurrencyCodeValidationException>(() => _sut.DefaultCurrencySetAsync(code, ct));
    }

    [Fact]
    public async Task ApiSettingsService_ReturnSettings()
    {
        // Arrange
        var ct = CancellationToken.None;
        var settings = new CurrencyApiSetting
        {
            Id = 0,
            DefaultCurrency = "RUB",
            CurrencyRoundCount = 10
        };
        await _mockDbContext.CurrencyApiSettings.AddAsync(settings, ct);
        await _mockDbContext.SaveChangesAsync(ct);
        
        // Act
        var defaultCurrency = await _sut.GetDefaultCurrencyAsync(ct);
        var currencyRoundCount = await _sut.GetCurrencyRoundCountAsync(ct);
        
        // 
        // Assert
        
        Assert.Equal(settings.DefaultCurrency, defaultCurrency);
        Assert.Equal(settings.CurrencyRoundCount, currencyRoundCount);
    }

    [Fact]
    public async Task ApiSettingsService_SetSettingsSuccessfully()
    {
        // Arrange
        var ct = CancellationToken.None;
        var settings = new CurrencyApiSetting
        {
            Id = 0,
            DefaultCurrency = "RUB",
            CurrencyRoundCount = 10
        };
        await _mockDbContext.CurrencyApiSettings.AddAsync(settings, ct);
        await _mockDbContext.SaveChangesAsync(ct);
        
        var newDefaultCurrency = "USD";
        var newCount = 20;
        
        // Act 
        
        await _sut.DefaultCurrencySetAsync(newDefaultCurrency, ct);
        await _sut.RoundCountSetAsync(newCount, ct);
        
        // Assert
        
        Assert.Equal(newCount, _mockDbContext.CurrencyApiSettings.FirstOrDefault()!.CurrencyRoundCount);
        Assert.Equal(newDefaultCurrency, _mockDbContext.CurrencyApiSettings.FirstOrDefault()!.DefaultCurrency);
    }

    [Fact]
    public async Task ApiSettingsService_ThrowException_WhenSettingsNotExistInDb()
    {
        // Arrange
        var ct = CancellationToken.None;
        
        await Assert.ThrowsAsync<ApiSettingsAreNotSetException>( () => _sut.GetDefaultCurrencyAsync(ct) );
        await Assert.ThrowsAsync<ApiSettingsAreNotSetException>( () => _sut.GetCurrencyRoundCountAsync(ct) );
    }
}