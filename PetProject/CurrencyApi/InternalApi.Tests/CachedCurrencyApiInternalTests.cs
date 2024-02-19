using InternalApi.Configuration;
using InternalApi.Contracts;
using InternalApi.Dtos;
using InternalApi.Entities;
using InternalApi.Infrastructure.Data.CurrencyContext;
using InternalApi.Services.Cache;
using InternalApi.Tests.Fixtures;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace InternalApi.Tests;

// Я написал эти тесты для внутренних методов сервиса кэша, чтобы убедиться в их правильной работе.

[Collection("Container Collection")]
public class CachedCurrencyApiInternalTests : IDisposable
{
    private CachedCurrencyApi _sut;
    private readonly Mock<IMemoryCache> _memoryCacheMock = new();
    private readonly Mock<ILogger<CachedCurrencyApi>> _loggerMock = new();
    private readonly Mock<ICurrencyApi> _externalApiMock = new();
    private readonly Mock<IOptionsMonitor<CurrencyCacheSettings>> _cacheOptionsMock = new();
    private readonly RenewalDatesDictionary _lockerDictionary = new();
    private AppDbContext _dbContext;
    private DatabaseFixture _fixture;

    private readonly CurrencyCacheSettings _cacheSettingsMock = new()
    {
        CacheExpirationHours = 24, BaseCurrency = CurrencyType.USD
    };

    public CachedCurrencyApiInternalTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _cacheOptionsMock.Setup(o => o.CurrentValue).Returns(_cacheSettingsMock);
        _dbContext = fixture.CreateAppContext();
        object? expectedOut = new();
        _memoryCacheMock.Setup(c => c.TryGetValue(It.IsAny<object>(), out expectedOut)).Returns(true);
        var cacheEntry = Mock.Of<ICacheEntry>();
        _memoryCacheMock.Setup(c => c.CreateEntry(It.IsAny<object>())).Returns(cacheEntry);
        _sut = new CachedCurrencyApi(_loggerMock.Object, _externalApiMock.Object, _cacheOptionsMock.Object, _dbContext,
            _lockerDictionary, _memoryCacheMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _fixture.Cleanup();
    }

    [Fact]
    public async Task GetEntity_ReturnCurrentEntity()
    {
        // Arrange
        var currencyType = CurrencyType.USD;
        var ct = CancellationToken.None;
        var entity = new Currency { Id = 0, Code = CurrencyType.USD, Value = 100, RateDate = DateTime.Now };
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
        var targetDate = DateTimeOffset.UtcNow.AddDays(-10);
        var entity = new Currency
        {
            Id = 0, Code = CurrencyType.USD, Value = 100, RateDate = targetDate
        };
        _dbContext.Currencies.Add(entity);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var currency = await _sut.GetEntityAsync(currencyType, DateOnly.FromDateTime(targetDate.Date), ct);

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
            Code = CurrencyType.USD, Value = 10, RateDate = targetDate.ToDateTime(new TimeOnly())
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
        var targetDate = DateTimeOffset.UtcNow;
        var cancellationToken = CancellationToken.None;
        var apiDto = new RootCurrencyApiDto
        {
            Meta = new MetaCurrencyApiDto { LastUpdatedAt = targetDate.Date },
            Data = new Dictionary<string, CurrencyApiDto>
            {
                { "USD", new CurrencyApiDto { Code = "USD", Value = 10 } }
            }
        };
        _externalApiMock.Setup(a =>
                a.GetAllCurrentCurrenciesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(apiDto));

        // Act

        // 100 потому, что на 10 потоках не всегда корректно тестировалась очистка словаря.
        var tasks = Enumerable.Range(1, 100).Select(async _ =>
        {
            await using (_dbContext = _fixture.CreateAppContext())
            {
                _sut = new CachedCurrencyApi(_loggerMock.Object, _externalApiMock.Object, _cacheOptionsMock.Object, _dbContext,
                    _lockerDictionary, _memoryCacheMock.Object);
                
                await _sut.SecureUpdateCacheAsync(currencyType, null, cancellationToken);
            }
        });
        await Task.WhenAll(tasks);

        //Assert
        _externalApiMock.Verify(
            a => a.GetAllCurrentCurrenciesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.True(_lockerDictionary.RenewalDatesLockDictionary.IsEmpty);
    }
}