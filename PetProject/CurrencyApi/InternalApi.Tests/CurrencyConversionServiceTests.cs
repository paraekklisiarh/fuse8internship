using InternalApi.Configuration;
using InternalApi.Entities;
using InternalApi.Infrastructure.Data.CurrencyContext;
using InternalApi.Services;
using InternalApi.Services.CurrencyConversion;
using InternalApi.Tests.Fixtures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;
using ILogger = Serilog.ILogger;

namespace InternalApi.Tests;

[Collection("Container Collection")]
public class CurrencyConversionServiceTests : IDisposable
{
    private readonly ICurrencyConversionService _sut;
    private readonly Mock<ILogger<CurrencyConversionService>> _loggerMock;
    private DatabaseFixture AppDbContextDatabaseFixture { get; }

    private readonly AppDbContext _appDbContext;

    Mock<IConfigurationSection> _mockSection = new Mock<IConfigurationSection>();
    private readonly Mock<IConfiguration> _configurationMock;

    private readonly Mock<IOptionsMonitor<CurrencyCacheSettings>> _cacheOptionsMock = new();

    private readonly CurrencyCacheSettings _cacheSettingsMock = new()
    {
        CacheExpirationHours = 24,
        BaseCurrency = CurrencyType.USD
    };

    public CurrencyConversionServiceTests(
        DatabaseFixture appDbContextDatabaseFixture
    )
    {
        _cacheOptionsMock
            .Setup(o => o.CurrentValue)
            .Returns(_cacheSettingsMock);

        AppDbContextDatabaseFixture = appDbContextDatabaseFixture;
        _appDbContext = AppDbContextDatabaseFixture.CreateAppContext();

        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<CurrencyConversionService>>();

        _sut = new CurrencyConversionService(
            dbContext: _appDbContext,
            cacheSettings: _cacheOptionsMock.Object,
            logger: _loggerMock.Object,
            configuration: _configurationMock.Object
        );
    }


    public void Dispose()
    {
        _appDbContext.Dispose();
        AppDbContextDatabaseFixture.Cleanup();
    }

    public async Task CurrencyConversionService_ConversedValues()
    {
        // Arrange
        _mockSection.Setup(x => x.Value).Returns("");
        _configurationMock.SetupGet(x => x["Cache:CurrencyAPICache:BaseCurrency"])
            .Returns("");
        _configurationMock.SetupSet(x => x["Cache:CurrencyAPICache:BaseCurrency"]);

        var ct = CancellationToken.None;
        
        var newBaseCurrency = CurrencyType.EUR;
        var task = new CurrencyConversionTask
        {
            Id = default,
            Status = CurrencyConversionStatus.Created,
            NewBaseCurrency = newBaseCurrency,
            StartTime = default,
            EndTime = null
        };

        var date1 = DateTime.UtcNow.AddHours(-1);
        var date2 = DateTime.UtcNow.AddDays(-1);
        var oldValues = new List<Currency>
        {
            new Currency { Code = CurrencyType.USD, Value = 1m, RateDate = date1 },
            new Currency { Code = CurrencyType.RUB, Value = 100m, RateDate = date1 },
            new Currency { Code = CurrencyType.EUR, Value = 0.9m, RateDate = date1 },
            new Currency { Code = CurrencyType.USD, Value = 1m, RateDate = date2 },
            new Currency { Code = CurrencyType.RUB, Value = 100m, RateDate = date2 },
            new Currency { Code = CurrencyType.EUR, Value = 0.9m, RateDate = date2 },
        };

        // Act

        await _sut.CurrencyConversion(task.Id, ct);
        
        // Assert
        Assert.Equal(1m, _appDbContext.Currencies.First(c => c.Code == CurrencyType.EUR).Value);
        
    }
}