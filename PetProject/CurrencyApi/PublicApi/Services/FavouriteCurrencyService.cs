using System.Globalization;
using AutoMapper;
using CurrencyApi;
using Fuse8_ByteMinds.SummerSchool.PublicApi.Dtos;
using Fuse8_ByteMinds.SummerSchool.PublicApi.Models;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Enum = System.Enum;
using FavouriteCurrency = Fuse8_ByteMinds.SummerSchool.PublicApi.Models.FavouriteCurrency;

namespace Fuse8_ByteMinds.SummerSchool.PublicApi.Services;

/// <summary>
///     Сервис управления избранными валютами
/// </summary>
public interface IFavouriteCurrencyService
{
    /// <summary>
    ///     Получить Избранное по его названию
    /// </summary>
    /// <param name="name">Название избранного</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Курс избранной валюты</returns>
    public Task<FavouriteCurrency> GetFavouriteCurrencyAsync(string name, CancellationToken cancellationToken);

    /// <summary>
    ///     Получить список всех Избранных
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Список всех избранных валют</returns>
    public Task<IEnumerable<FavouriteCurrency>> GetFavouritesCurrenciesAsync(CancellationToken cancellationToken);


    /// <summary>
    ///     Добавить новое Избранное
    /// </summary>
    /// <param name="newFavouriteCurrencyDto">Добавляемое избранное</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>true, если успешно, иначе false</returns>
    public Task AddFavouriteCurrencyAsync(FavouriteCurrencyDto newFavouriteCurrencyDto,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Изменить Избранное по его названию
    /// </summary>
    /// <param name="name">Название изменяемого избранного</param>
    /// <param name="editedFavouriteCurrencyDto">Модифицированный объект избранной валюты</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>true, если успешно, иначе false</returns>
    public Task EditFavouriteCurrencyAsync(string name, FavouriteCurrencyDto editedFavouriteCurrencyDto,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Удалить Избранное по его названию
    /// </summary>
    /// <param name="name">Название удаляемого избранного</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>true, если успешно, иначе false</returns>
    public Task DeleteFavouriteCurrencyAsync(string name, CancellationToken cancellationToken);

    /// <summary>
    ///     Получение текущего курса Избранного по его названию
    /// </summary>
    /// <param name="name">Название элемента избранного</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Текущий курс валюты элемента избранного</returns>
    public Task<Currency> GetFavouriteCurrencyCurrentAsync(string name, CancellationToken cancellationToken);

    /// <summary>
    ///     Получение курса Избранного по его названию на конкретную дату
    /// </summary>
    /// <param name="name">Название элемента избранного</param>
    /// <param name="targetDate">Дата курса валюты</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Курс валюты элемента избранного на <see cref="targetDate" /></returns>
    public Task<Currency> GetFavouriteCurrencyOnDateAsync(string name, DateOnly targetDate,
        CancellationToken cancellationToken);
}

/// <inheritdoc />
public class FavouriteCurrencyService : IFavouriteCurrencyService
{
    private readonly IMapper _mapper;
    private readonly AppDbContext _dbContext;
    private readonly GetCurrency.GetCurrencyClient _getCurrency;
    private readonly IApiSettingsService _settings;

    /// <summary>
    ///     Конструктор сервиса
    /// </summary>
    /// <param name="dbContext">БД</param>
    /// <param name="getCurrency">gRPC клиент</param>
    /// <param name="settings">Настройки API</param>
    /// <param name="mapper">AutoMapper</param>
    public FavouriteCurrencyService(AppDbContext dbContext, GetCurrency.GetCurrencyClient getCurrency,
        IApiSettingsService settings, IMapper mapper)
    {
        _dbContext = dbContext;
        _getCurrency = getCurrency;
        _settings = settings;
        _mapper = mapper;
    }

    /// <inheritdoc />
    /// <exception cref="FavouriteCurrencyNotFoundException">Выбрасывается, если избранное <see cref="name" /> не найдено.</exception>
    public async Task<FavouriteCurrency> GetFavouriteCurrencyAsync(string name, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var response = await _dbContext
            .FavouriteCurrencies
            .Where(f => f.Name == name)
            .FirstOrDefaultAsync(cancellationToken);

        if (response == null)
            throw new FavouriteCurrencyNotFoundException(
                $"Не существует избранной валюты {name}");

        return response;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<FavouriteCurrency>> GetFavouritesCurrenciesAsync(CancellationToken cancellationToken)
    {
        var response = await _dbContext.FavouriteCurrencies.ToListAsync(cancellationToken);

        return response;
    }

    /// <inheritdoc />
    /// <exception cref="NotUniqueFavouriteCurrencyException">Выбрасывается, если имя не уникально.</exception>
    /// <exception cref="NotUniqueFavouriteCurrencyException">
    ///     Выбрасывается, если сочетание базовой и основной валюты не
    ///     уникально.
    /// </exception>
    public async Task AddFavouriteCurrencyAsync(FavouriteCurrencyDto dto,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var newFavouriteCurrency = _mapper.Map<FavouriteCurrency>(dto);

        if (await _dbContext.FavouriteCurrencies.AnyAsync(f =>
                f.Name == newFavouriteCurrency.Name, cancellationToken))
            throw new NotUniqueFavouriteCurrencyException("Имя избранной валюты должно быть уникальным");

        if (await _dbContext.FavouriteCurrencies.AnyAsync(f =>
                f.Currency == newFavouriteCurrency.Currency &&
                f.BaseCurrency == newFavouriteCurrency.BaseCurrency, cancellationToken))
            throw new NotUniqueFavouriteCurrencyException(
                "Сочетание базовой валюты и основной валюты должно быть уникальным");

        await _dbContext.FavouriteCurrencies.AddAsync(newFavouriteCurrency, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    /// <exception cref="FavouriteCurrencyNotFoundException">Выбрасывается, если избранное <see cref="name" /> не найдено.</exception>
    /// <exception cref="NotUniqueFavouriteCurrencyException">Выбрасывается, если имя не уникально.</exception>
    /// <exception cref="NotUniqueFavouriteCurrencyException">
    ///     Выбрасывается, если сочетание базовой и основной валюты не
    ///     уникально.
    /// </exception>
    public async Task EditFavouriteCurrencyAsync(string name, FavouriteCurrencyDto dto,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var entity = await GetFavouriteCurrencyAsync(name, cancellationToken);

        var editedFavouriteCurrency = _mapper.Map<FavouriteCurrency>(dto);

        entity.Currency = editedFavouriteCurrency.Currency;
        entity.BaseCurrency = editedFavouriteCurrency.BaseCurrency;
        entity.Name = editedFavouriteCurrency.Name;

        if (await _dbContext.FavouriteCurrencies.AnyAsync(f =>
                f != entity &&
                f.Name == editedFavouriteCurrency.Name, cancellationToken))
            throw new NotUniqueFavouriteCurrencyException("Имя избранной валюты должно быть уникальным");

        if (await _dbContext.FavouriteCurrencies.AnyAsync(f =>
                f != entity &&
                f.Currency == editedFavouriteCurrency.Currency &&
                f.BaseCurrency == editedFavouriteCurrency.BaseCurrency, cancellationToken))
            throw new NotUniqueFavouriteCurrencyException(
                "Сочетание базовой валюты и основной валюты избранного должно быть уникальным");

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    /// <exception cref="FavouriteCurrencyNotFoundException">Выбрасывается, если избранное <see cref="name" /> не найдено.</exception>
    public async Task DeleteFavouriteCurrencyAsync(string name, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var favouriteCurrency = await _dbContext
            .FavouriteCurrencies
            .Where(f => f.Name == name)
            .FirstOrDefaultAsync(cancellationToken);

        if (favouriteCurrency == null)
            throw new FavouriteCurrencyNotFoundException(
                $"Не существует избранной валюты {name}");

        _dbContext.FavouriteCurrencies.Remove(favouriteCurrency);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Currency> GetFavouriteCurrencyCurrentAsync(string name, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var entity = await GetFavouriteCurrencyAsync(name, cancellationToken);

        Enum.TryParse(entity.Currency, out CurrencyTypeDTO currencyType);
        Enum.TryParse(entity.BaseCurrency, out CurrencyTypeDTO baseCurrencyType);
        CurrencyApi.FavouriteCurrency request = new()
        {
            CurrencyType = currencyType,
            BaseCurrencyType = baseCurrencyType
        };
        var dto = await _getCurrency.GetFavouriteCurrencyCurrentAsync(request,
            cancellationToken: cancellationToken);

        var currency = await ParseCurrencyDto(dto, cancellationToken);

        return currency;
    }

    /// <inheritdoc />
    public async Task<Currency> GetFavouriteCurrencyOnDateAsync(string name, DateOnly targetDate,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var entity = await GetFavouriteCurrencyAsync(name, cancellationToken);

        Enum.TryParse(entity.Currency, out CurrencyTypeDTO currencyType);
        Enum.TryParse(entity.BaseCurrency, out CurrencyTypeDTO baseCurrencyType);

        FavouriteCurrencyOnDate request = new()
        {
            CurrencyType = currencyType,
            BaseCurrencyType = baseCurrencyType,
            Date = targetDate.ToDateTime(new TimeOnly()).ToUniversalTime().ToTimestamp()
        };
        var dto = await _getCurrency.GetFavouriteCurrencyOnDateAsync(request,
            cancellationToken: cancellationToken);

        var currency = await ParseCurrencyDto(dto, cancellationToken);

        return currency;
    }

    /// <summary>
    ///     Парсинг Dto
    /// </summary>
    /// <param name="dto">Currency Dto</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Объект <see cref="Currency" /></returns>
    private async Task<Currency> ParseCurrencyDto(CurrencyDTO dto, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var value = decimal.Parse(dto.Value, CultureInfo.InvariantCulture);

        return new Currency
        {
            Code = dto.CurrencyType.ToString().ToUpper(),
            Value = Math.Round(value, await _settings.GetCurrencyRoundCountAsync(cancellationToken))
        };
    }
}

/// <inheritdoc />
public class NotUniqueFavouriteCurrencyException : Exception
{
    /// <inheritdoc />
    public NotUniqueFavouriteCurrencyException(string? message) : base(message)
    {
    }
}

/// <inheritdoc />
public class FavouriteCurrencyNotFoundException : Exception
{
    /// <inheritdoc />
    public FavouriteCurrencyNotFoundException(string? message) : base(message)
    {
    }
}