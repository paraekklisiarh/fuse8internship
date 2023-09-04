using InternalApi.Configuration;
using InternalApi.Contracts;
using InternalApi.Dtos;
using InternalApi.Entities;
using InternalApi.Infrastructure;
using InternalApi.Infrastructure.Data.CurrencyContext;
using InternalApi.Services;
using InternalApi.Services.Cache;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;


namespace InternalApi.Tests;

[Collection("TransactionalTests")]
public class CachedCurrencyApiTests : IDisposable
{
    private CachedCurrencyApi _sut;

    private readonly Mock<IMemoryCache> _memoryCacheMock = new();
    
    private readonly Mock<ILogger<CachedCurrencyApi>> _loggerMock = new();
    private readonly Mock<ICurrencyApi> _externalApiMock = new();
    private readonly Mock<IOptionsMonitor<CurrencyCacheSettings>> _cacheOptionsMock = new();
    private readonly RenewalDatesDictionary _lockerDictionary = new();

    private AppDbContext _dbContext;

    private readonly CurrencyCacheSettings _cacheSettingsMock = new()
    {
        CacheExpirationHours = 24,
        BaseCurrency = CurrencyType.USD
    };
    
    public TestAppDbContextDatabaseFixture Fixture { get; }

    public CachedCurrencyApiTests(TestAppDbContextDatabaseFixture fixture)
    {
        Fixture = fixture;
        _dbContext = fixture.CreateContext();

        _cacheOptionsMock
            .Setup(o => o.CurrentValue)
            .Returns(_cacheSettingsMock);

        object? expectedOut = new();
        _memoryCacheMock.Setup(c => c.TryGetValue( It.IsAny<object>(), out expectedOut))
            .Returns(true);
        var cacheEntry = Mock.Of<ICacheEntry>();
        _memoryCacheMock.Setup(c => c.CreateEntry(It.IsAny<object>()))
            .Returns(cacheEntry);

        _sut = new CachedCurrencyApi(_loggerMock.Object, _externalApiMock.Object, _cacheOptionsMock.Object,
            _dbContext, _lockerDictionary, _memoryCacheMock.Object);
    }

    public void Dispose()
    {
        Fixture.Cleanup();
    }

    [Fact]
    public async Task GetCurrentCurrencyAsync_ReturnCurrencyFromInMemoryCache()
    {
        // Arrange
        var currencyType = CurrencyType.USD;
        decimal value = 100;
        var ct = CancellationToken.None;

        var entity = new Currency
        {
            Id = 0,
            Code = currencyType,
            Value = value,
            RateDate = DateTime.Now
        };
        object expectedOut = entity;
        _memoryCacheMock.Setup(c => c.TryGetValue( It.IsAny<object>(), out expectedOut!))
            .Returns(true);
        
        // Act

        var actual = await _sut.GetCurrentCurrencyAsync(currencyType, ct);

        // Assert

        Assert.Equal(entity, actual);
        _externalApiMock.Verify(
            api => api.GetAllCurrentCurrenciesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.False(await _dbContext.Currencies.AnyAsync(cancellationToken: ct));
    }
    
    [Fact]
    public async Task GetCurrentCurrencyAsync_ReturnCurrencyFromDbCache()
    {
        // Arrange
        var currencyType = CurrencyType.USD;
        decimal value = 100;
        var ct = CancellationToken.None;

        var entity = new Currency
        {
            Id = 0,
            Code = currencyType,
            Value = value,
            RateDate = DateTime.Now
        };

        _dbContext.Currencies.Add(entity);
        await _dbContext.SaveChangesAsync(ct);
        
        // Act

        var actual = await _sut.GetCurrentCurrencyAsync(currencyType, ct);

        // Assert

        Assert.Equal(entity, actual);
        _externalApiMock.Verify(
            api => api.GetAllCurrentCurrenciesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _memoryCacheMock.Verify(c => c.CreateEntry(It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task GetCurrentCurrencyAsync_ReturnNewCurrencyFromApi_WhenCurrencyNotFoundInCache()
    {
        // Arrange
        var currencyType = CurrencyType.USD;
        decimal value = 100;
        var date = DateTime.Now.AddHours(-1);
        var ct = CancellationToken.None;

        var entity = new Currency
        {
            Code = currencyType,
            Value = value,
            RateDate = date
        };
        var apiResponse = new RootCurrencyApiDto
        {
            Meta = new MetaCurrencyApiDto { LastUpdatedAt = DateTime.Now }, Data = new()
        };
        apiResponse.Data.Add(currencyType.ToString(),
            new CurrencyApiDto { Code = currencyType.ToString(), Value = value });

        _externalApiMock.Setup(api => api
                .GetAllCurrentCurrenciesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => apiResponse);

        // Act
        await _dbContext.Database.BeginTransactionAsync(ct);
        
        var actual = await _sut.GetCurrentCurrencyAsync(currencyType, ct);

        _dbContext.ChangeTracker.Clear();
        // Assert

        _externalApiMock.Verify(
            api => api.GetAllCurrentCurrenciesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Equal(entity.Code, actual.Code);
        Assert.Equal(entity.Value, actual.Value);
    }

    
    // TODO: тест ломается из-за многопоточности.
    // Найди способ симулировать поведение на проде. Может создавать новые экземпляры? Контекста? Сервиса? :(
    
    //[Fact]
    public async Task GetCurrentCurrencyAsync_CollisionFree()
    {
        // Arrange
        var currencyType = CurrencyType.USD;
        decimal value = 100;
        var ct = CancellationToken.None;
        
        var apiResponse = new RootCurrencyApiDto
        {
            Meta = new MetaCurrencyApiDto { LastUpdatedAt = DateTime.Now }, Data = new()
        };
        apiResponse.Data.Add(currencyType.ToString(),
            new CurrencyApiDto { Code = currencyType.ToString(), Value = value });

        _externalApiMock.Setup(api => api
                .GetAllCurrentCurrenciesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => apiResponse);

        // Act
        var tasks = Enumerable.Range(1, 10)
            .Select(_ => { return Task.Run(() =>
            {
                _dbContext = Fixture.CreateContext();
                _sut = new CachedCurrencyApi(_loggerMock.Object, _externalApiMock.Object, _cacheOptionsMock.Object,
                    _dbContext, _lockerDictionary, _memoryCacheMock.Object);
                
                return _sut.GetCurrentCurrencyAsync(currencyType, ct);
            }, ct); });
        await Task.WhenAll(tasks);

        //var actual = await _sut.GetCurrentCurrencyAsync(currencyType, ct);

        // Assert
        //Assert.Equal(currencyType, actual.Code);
        //Assert.Equal(value, actual.Value);

        _externalApiMock.Verify(
            api => api.GetAllCurrentCurrenciesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.True(_lockerDictionary.RenewalDatesLockDictionary.IsEmpty);
    }

    [Fact]
    public async Task GetCurrencyOnDateAsync_ReturnCurrencyFromInMemoryCache()
    {
        // Arrange
        var currencyType = CurrencyType.USD;
        decimal value = 100;
        var date = DateTime.UtcNow.AddDays(-100);
        var ct = CancellationToken.None;

        var entity = new Currency
        {
            Id = 0,
            Code = currencyType,
            Value = value,
            RateDate = date
        };
        object expectedOut = entity;
        _memoryCacheMock.Setup(c => c.TryGetValue( It.IsAny<object>(), out expectedOut!))
            .Returns(true);
        
        // Act

        var actual = await _sut.GetCurrencyOnDateAsync(currencyType, DateOnly.FromDateTime(date), ct);

        // Assert

        Assert.Equal(entity, actual);
        
        _externalApiMock.Verify(api => api.GetAllCurrenciesOnDateAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>() ),
            Times.Never);
        Assert.False(await _dbContext.Currencies.AnyAsync(cancellationToken: ct));
    }
    
    [Fact]
    public async Task GetCurrencyOnDateAsync_ReturnCurrencyFromDbCache()
    {
        // Arrange
        var currencyType = CurrencyType.USD;
        decimal value = 100;
        var date = DateTime.UtcNow.AddDays(-100);
        var ct = CancellationToken.None;

        var entity = new Currency
        {
            Id = 0,
            Code = currencyType,
            Value = value,
            RateDate = date
        };

        _dbContext.Currencies.Add(entity);
        await _dbContext.SaveChangesAsync(ct);

        // Act

        var actual = await _sut.GetCurrencyOnDateAsync(currencyType, DateOnly.FromDateTime(date), ct);

        // Assert

        Assert.Equal(entity, actual);
        
        _externalApiMock.Verify(api => api.GetAllCurrenciesOnDateAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>() ),
            Times.Never);
    }

    [Fact]
    public async Task GetCurrencyOnDateAsync_ReturnNewCurrencyFromApi_WhenCurrencyNotFoundInCache()
    {
        // Arrange
        var currencyType = CurrencyType.USD;
        decimal value = 100;
        var date = DateTime.Now.AddHours(-1).AddDays(-100);
        var ct = CancellationToken.None;

        var entity = new Currency
        {
            Code = currencyType,
            Value = value,
            RateDate = date
        };
        var apiResponse = new RootCurrencyApiDto
        {
            Meta = new MetaCurrencyApiDto { LastUpdatedAt = date }, Data = new()
        };
        apiResponse.Data.Add(currencyType.ToString(),
            new CurrencyApiDto { Code = currencyType.ToString(), Value = value });

        _externalApiMock.Setup(api => api
                .GetAllCurrenciesOnDateAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => apiResponse);

        // Act
        await _dbContext.Database.BeginTransactionAsync(ct);
        
        var actual = await _sut.GetCurrencyOnDateAsync(currencyType, DateOnly.FromDateTime(date), ct);

        _dbContext.ChangeTracker.Clear();
        // Assert

        Assert.Equal(entity.Code, actual.Code);
        Assert.Equal(entity.Value, actual.Value);
        _externalApiMock.Verify(api => api.GetAllCurrenciesOnDateAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>() ),
            Times.Once);
    }

    //[Fact]
    public async Task GetCurrencyOnDateAsync_CollisionFree()
    {
    }
}