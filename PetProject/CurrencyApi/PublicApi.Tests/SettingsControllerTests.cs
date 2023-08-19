using Fuse8_ByteMinds.SummerSchool.PublicApi.Controllers;
using Fuse8_ByteMinds.SummerSchool.PublicApi.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace PublicApi.Tests;

public class SettingsControllerTests
{
    private readonly Mock<IApiSettingsService> _settingsServiceMock;

    private readonly SettingsController _sut;

    public SettingsControllerTests()
    {
        _settingsServiceMock = new Mock<IApiSettingsService>();
        _sut = new SettingsController(_settingsServiceMock.Object);
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
        
        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task SetRoundCount_ReturnOk()
    {
        // Arrange
        var ct = CancellationToken.None;
        var count = -100;

        _settingsServiceMock.Setup(s => s.RoundCountSetAsync(count, ct))
            .Returns(Task.CompletedTask);

        // Act

        var result = await _sut.SetRoundCount(count, ct);

        // Assert
        
        Assert.IsType<OkResult>(result);
    }
}