using Fuse8_ByteMinds.SummerSchool.PublicApi;
using Fuse8_ByteMinds.SummerSchool.PublicApi.Models;
using Fuse8_ByteMinds.SummerSchool.PublicApi.Services;
using Microsoft.EntityFrameworkCore;

namespace PublicApi.Tests;

public class ApiSettingsServiceTests
{
    private readonly AppDbContext _mockDbContext;
    private readonly CurrencyApiSettings _apiSettingsMock;

    private readonly ApiSettingsService _sut;

    public ApiSettingsServiceTests()
    {
        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _mockDbContext = new AppDbContext(dbOptions);

        _apiSettingsMock = new CurrencyApiSettings(_mockDbContext);
        
        _sut = new ApiSettingsService(_apiSettingsMock);
    }

    public void Dispose()
    {
        _mockDbContext.Dispose();
    }

    [Fact]
    public async Task DefaultCurrencySetAsync_SetValueSuccessfully()
    {
        // Arrange
        var ct = CancellationToken.None;
        var code = "RUB";

        // Act
        await _sut.DefaultCurrencySetAsync(code, ct);

        var actual = await _apiSettingsMock.GetDefaultCurrencyAsync(ct);
        
        // Assert
        
        Assert.Equal(code, actual);
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
    public async Task RoundCountSetAsync_SetValueSuccessfully()
    {
        // Arrange
        var ct = CancellationToken.None;
        var count = 10;

        // Act
        await _sut.RoundCountSetAsync(count, ct);

        var actual = await _apiSettingsMock.GetCurrencyRoundCountAsync(ct);
        
        // Assert
        
        Assert.Equal(count, actual);
    }
}