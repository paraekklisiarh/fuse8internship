using CurrencyApi;
using Fuse8_ByteMinds.SummerSchool.PublicApi.Controllers;
using Fuse8_ByteMinds.SummerSchool.PublicApi.Dtos;
using Fuse8_ByteMinds.SummerSchool.PublicApi.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace PublicApi.Tests;

public class SettingsControllerTests
{
    private readonly Mock<IApiSettingsService> _settingsServiceMock;
    private readonly Mock<ICurrencyService> _currencyServiceMock;

    private readonly SettingsController _sut;

    public SettingsControllerTests()
    {
        _settingsServiceMock = new Mock<IApiSettingsService>();
        _currencyServiceMock = new Mock<ICurrencyService>();
        _sut = new SettingsController(_settingsServiceMock.Object, _currencyServiceMock.Object);
    }

    [Fact]
    public async Task DefaultCurrencySet_ReturnOk()
    {
        // Arrange
        var ct = CancellationToken.None;
        var defaultCurrency = "RUB";

        _settingsServiceMock.Setup(s => s.DefaultCurrencySetAsync(defaultCurrency, ct))
            .Returns(Task.CompletedTask);

        // Act

        var result = await _sut.DefaultCurrencySet(defaultCurrency, ct);

        // Assert
        _settingsServiceMock.Verify(m => m.DefaultCurrencySetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task SetRoundCount_ReturnOk()
    {
        // Arrange
        var ct = CancellationToken.None;
        var count = 100;

        _settingsServiceMock.Setup(s => s.RoundCountSetAsync(count, ct))
            .Returns(Task.CompletedTask);

        // Act

        var result = await _sut.SetRoundCount(count, ct);

        // Assert
        
        _settingsServiceMock.Verify(m => m.RoundCountSetAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task GetSettings_ReturnSettings()
    {
        // Arrange
        var ct = CancellationToken.None;
        var defaultCurrency = "RUB";
        _settingsServiceMock.Setup(s => s.GetDefaultCurrencyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultCurrency);
        
        var count = 100;
        _settingsServiceMock.Setup(s => s.GetCurrencyRoundCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(count);
        
        var serverSettings = new Settings
        {
            BaseCurrency = "BC",
            NewRequestsAvailable = false
        };
        _currencyServiceMock.Setup(s => s.GetCurrencyServerSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(serverSettings);

        // Act
        var result = await _sut.GetSettings(ct);

        // Assert

        var objectResult = Assert.IsType<OkObjectResult>(result);

        var model = Assert.IsAssignableFrom<SettingsDto>(objectResult.Value);
        
        Assert.Equal(defaultCurrency, model.DefaultCurrency);
        Assert.Equal(count, model.CurrencyRoundCount);
        
        Assert.Equal(serverSettings.BaseCurrency, model.BaseCurrency);
        Assert.Equal(serverSettings.NewRequestsAvailable, model.NewRequestsAvailable);
        
    }
    
    [Fact]
    public async Task GetSettings_ReturnSettings_WhenApiSettingsNotSet()
    {
        // Arrange
        var ct = CancellationToken.None;
        
        _settingsServiceMock.Setup(s => s.GetDefaultCurrencyAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ApiSettingsAreNotSetException(""));
        _settingsServiceMock.Setup(s => s.GetCurrencyRoundCountAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ApiSettingsAreNotSetException(""));
        
        var serverSettings = new Settings
        {
            BaseCurrency = "BC",
            NewRequestsAvailable = false
        };
        _currencyServiceMock.Setup(s => s.GetCurrencyServerSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(serverSettings);

        // Act
        var result = await _sut.GetSettings(ct);

        // Assert

        var objectResult = Assert.IsType<OkObjectResult>(result);

        var model = Assert.IsAssignableFrom<SettingsDto>(objectResult.Value);
        
        Assert.Null(model.DefaultCurrency);
        
        Assert.Equal(serverSettings.BaseCurrency, model.BaseCurrency);
        Assert.Equal(serverSettings.NewRequestsAvailable, model.NewRequestsAvailable);
        
    }
}