using InternalApi.Contracts;
using InternalApi.Dtos;
using InternalApi.Entities;
using InternalApi.Infrastructure;
using InternalApi.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace InternalApi.Tests;

// Я написал эти тесты для внутренних методов сервиса кэша, чтобы убедиться в их правильной работе.

[Collection("TransactionalTests")]
public class CachedCurrencyApiInternalTests : IDisposable
{
    private readonly CachedCurrencyApi _sut;

    private readonly Mock<ILogger<CachedCurrencyApi>> _loggerMock = new();
    private readonly Mock<ICurrencyApi> _externalApiMock = new();
    private readonly Mock<IOptionsMonitor<CurrencyCacheSettings>> _cacheOptionsMock = new();
    private readonly RenewalDatesDictionary _lockerDictionary = new();

    private readonly AppDbContext _dbContext;

    private readonly CurrencyCacheSettings _cacheSettingsMock = new()
    {
        CacheExpirationHours = 24,
        BaseCurrency = CurrencyType.USD
    };

    public CachedCurrencyApiInternalTests(TestDatabaseFixture fixture)
    {
        // dbContext mocking
        // DbContextOptions<AppDbContext> dbOptions = new DbContextOptionsBuilder<AppDbContext>()
        //     .UseInMemoryDatabase(databaseName: new Guid().ToString())
        //     .Options;
        // _dbContext = new AppDbContext(dbOptions);

        _cacheOptionsMock
            .Setup(o => o.CurrentValue)
            .Returns(_cacheSettingsMock);

        _dbContext = fixture.CreateContext();

        _sut = new CachedCurrencyApi(_loggerMock.Object, _externalApiMock.Object, _cacheOptionsMock.Object,
            _dbContext, _lockerDictionary);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public async Task GetEntity_ReturnCurrentEntity()
    {
        // Arrange
        var currencyType = CurrencyType.USD;
        var ct = CancellationToken.None;

        var entity = new Currency
        {
            Id = 0,
            Code = CurrencyType.USD,
            Value = 100,
            RateDate = DateTime.Now
        };

        _dbContext.Currencies.Add(entity);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var currency = await _sut.GetEntityAsync(currencyType, null, ct);

        // Assert
        Assert.Equal(entity, currency);
    }

    [Fact]
    public async Task GetEntity_ReturnEntityOnDate()
    {
        // Arrange
        var currencyType = CurrencyType.USD;
        var ct = CancellationToken.None;
        var targetDate = DateOnly.Parse("2000-02-02");

        var entity = new Currency
        {
            Id = 0,
            Code = CurrencyType.USD,
            Value = 100,
            RateDate = targetDate.ToDateTime(new TimeOnly())
        };

        _dbContext.Currencies.Add(entity);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var currency = await _sut.GetEntityAsync(currencyType, targetDate, ct);

        // Assert
        Assert.Equal(entity, currency);
    }

    [Fact]
    public async Task ParseEntity_CorrectParseCurrenciesOnDate()
    {
        // Arrange
        var targetDate = DateOnly.Parse("2020-02-02");

        var apiDto = new RootCurrencyApiDto
        {
            Meta = new MetaCurrencyApiDto { LastUpdatedAt = targetDate.ToDateTime(new TimeOnly()) },
            Data = new Dictionary<string, CurrencyApiDto>
            {
                { "USD", new CurrencyApiDto { Code = "USD", Value = 10 } }
            }
        };

        var expected = new Currency
        {
            Code = CurrencyType.USD,
            Value = 10,
            RateDate = targetDate.ToDateTime(new TimeOnly())
        };

        // Act

        var currency = _sut.ParseEntity(apiDto).FirstOrDefault();

        // Assert

        Assert.Equal(expected, currency);
    }

    [Fact]
    public async Task ThreadSafeUpdates_CollisionFree()
    {
        // Arrange
        var currencyType = CurrencyType.USD;
        var targetDate = DateOnly.Parse("2020-02-02");
        var cancellationToken = CancellationToken.None;

        var apiDto = new RootCurrencyApiDto
        {
            Meta = new MetaCurrencyApiDto { LastUpdatedAt = targetDate.ToDateTime(new TimeOnly()) },
            Data = new Dictionary<string, CurrencyApiDto>
            {
                { "USD", new CurrencyApiDto { Code = "USD", Value = 10 } }
            }
        };
        _externalApiMock.Setup(a =>
                a.GetAllCurrenciesOnDateAsync(It.IsAny<string>(), targetDate, It.IsAny<CancellationToken>()))
            .Returns(async () => apiDto);

        // Act

        // 100 потому, что на 10 потоках не всегда корректно тестировалась очистка словаря.
        var tasks = Enumerable.Range(1, 100)
            .Select(_ => Task.Run(
                () => _sut.SecureUpdateCacheAsync(currencyType, targetDate, cancellationToken), cancellationToken));
        await Task.WhenAll(tasks);

        //Assert

        _externalApiMock.Verify(
            a => a.GetAllCurrenciesOnDateAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()),
            Times.Once);

        Assert.True(_lockerDictionary.RenewalDatesLockDictionary.IsEmpty);
    }
}