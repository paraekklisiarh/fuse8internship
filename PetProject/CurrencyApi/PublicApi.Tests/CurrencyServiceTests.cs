using System.Globalization;
using CurrencyApi;
using Fuse8_ByteMinds.SummerSchool.PublicApi.Services;
using Grpc.Core;
using Moq;

namespace PublicApi.Tests;

public class CurrencyServiceTests
{
    private readonly Mock<IApiSettingsService> _currencyApiSettingsMock = new();
    private readonly Mock<GetCurrency.GetCurrencyClient> _getCurrencyClientMock = new();
    private readonly ICurrencyService _sut;
    
    public CurrencyServiceTests()
    {
        _sut = new CurrencyService(_getCurrencyClientMock.Object, _currencyApiSettingsMock.Object);
    }

    [Fact]
    public async Task GetCurrencyAsync_ReturnRoundedCurrency()
    {
        // Arrange
        var ct = CancellationToken.None;
        const CurrencyTypeDTO currencyType = CurrencyTypeDTO.Rub;
        const decimal value = 100.99999999m;
        const int roundCount = 2;
        
        var dto = new CurrencyDTO
        {
            CurrencyType = currencyType,
            Value = value.ToString(CultureInfo.InvariantCulture)
        };
        var grpcResponse = new AsyncUnaryCall<CurrencyDTO>(Task.FromResult(dto), Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess, () => new Metadata(), () => { });

        _getCurrencyClientMock.Setup(g => g.GetCurrencyAsync(It.IsAny<CurrencyApi.Code>(),
                It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(grpcResponse);

        // Act

        var actual = await _sut.GetCurrencyAsync(currencyType, ct);

        // Assert

        Assert.Equal(Math.Round(value, roundCount), actual.Value);
        Assert.Equal(currencyType.ToString().ToUpper(), actual.Code);
    }

    [Fact]
    public async Task GetDefaultCurrencyAsync_ReturnRoundedCurrency()
    {
        // Arrange
        var ct = CancellationToken.None;
        const CurrencyTypeDTO currencyType = CurrencyTypeDTO.Rub;
        _currencyApiSettingsMock.Setup(s => s.GetDefaultCurrencyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(currencyType.ToString);

        const decimal value = 100.99999999m;
        const int roundCount = 2;
        
        var dto = new CurrencyDTO
        {
            CurrencyType = currencyType,
            Value = value.ToString(CultureInfo.InvariantCulture)
        };
        var grpcResponse = new AsyncUnaryCall<CurrencyDTO>(Task.FromResult(dto), Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess, () => new Metadata(), () => { });

        _getCurrencyClientMock.Setup(g => g.GetCurrencyAsync(It.IsAny<CurrencyApi.Code>(),
                It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(grpcResponse);

        // Act

        var actual = await _sut.GetDefaultCurrencyAsync(ct);

        // Assert

        Assert.Equal(Math.Round(value, roundCount), actual.Value);
        Assert.Equal(currencyType.ToString().ToUpper(), actual.Code);
    }

    [Fact]
    public async Task GetCurrencyOnDateAsync_ReturnRoundedCurrency()
    {
        // Arrange
        var ct = CancellationToken.None;
        const CurrencyTypeDTO currencyType = CurrencyTypeDTO.Rub;
        const decimal value = 100.99999999m;
        const int roundCount = 2;
        var targetDate = DateOnly.Parse("2000-02-02");
        
        _currencyApiSettingsMock.Setup(s => s.GetCurrencyRoundCountAsync(ct))
            .ReturnsAsync(roundCount);

        var dto = new CurrencyDTO
        {
            CurrencyType = CurrencyTypeDTO.Rub,
            Value = value.ToString(CultureInfo.InvariantCulture)
        };
        var grpcResponse = new AsyncUnaryCall<CurrencyDTO>(Task.FromResult(dto), Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess, () => new Metadata(), () => { });

        _getCurrencyClientMock.Setup(g => g.GetCurrencyOnDateAsync(It.IsAny<CurrencyApi.CodeAndDate>(),
                It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(grpcResponse);

        // Act

        var actual = await _sut.GetCurrencyOnDateAsync(currencyType, targetDate, ct);

        // Assert

        Assert.Equal(Math.Round(value, roundCount), actual.Value);
        Assert.Equal(currencyType.ToString().ToUpper(), actual.Code);
    }
}