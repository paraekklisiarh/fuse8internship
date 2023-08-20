using Fuse8_ByteMinds.SummerSchool.PublicApi.Models;
using Microsoft.EntityFrameworkCore;

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
    /// <param name="newFavouriteCurrency">Добавляемое избранное</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>true, если успешно, иначе false</returns>
    public Task AddFavouriteCurrencyAsync(FavouriteCurrency newFavouriteCurrency,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Изменить Избранное по его названию
    /// </summary>
    /// <param name="name">Название изменяемого избранного</param>
    /// <param name="editedFavouriteCurrency">Модифицированный объект избранной валюты</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>true, если успешно, иначе false</returns>
    public Task EditFavouriteCurrencyAsync(string name, FavouriteCurrency editedFavouriteCurrency,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Удалить Избранное по его названию
    /// </summary>
    /// <param name="name">Название удаляемого избранного</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>true, если успешно, иначе false</returns>
    public Task DeleteFavouriteCurrencyAsync(string name, CancellationToken cancellationToken);
}

/// <inheritdoc />
public class FavouriteCurrencyService : IFavouriteCurrencyService
{
    private readonly AppDbContext _dbContext;
    private readonly GetCurrency.GetCurrencyClient _getCurrency;
    private readonly IApiSettingsService _settings;

    /// <summary>
    ///     Конструктор сервиса
    /// </summary>
    /// <param name="dbContext">БД</param>
    /// <param name="getCurrency">gRPC клиент</param>
    /// <param name="settings">Настройки API</param>
    public FavouriteCurrencyService(AppDbContext dbContext, GetCurrency.GetCurrencyClient getCurrency, IApiSettingsService settings)
    {
        _dbContext = dbContext;
        _getCurrency = getCurrency;
        _settings = settings;
    }

    /// <inheritdoc />
    public async Task<FavouriteCurrency> GetFavouriteCurrencyAsync(string name, CancellationToken cancellationToken)
    {
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
    /// <exception cref="NotUniqueFavouriteCurrency">Выбрасывается, если имя не уникально.</exception>
    /// <exception cref="NotUniqueFavouriteCurrency">Выбрасывается, если сочетание базовой и основной валюты не уникально.</exception>
    public async Task AddFavouriteCurrencyAsync(FavouriteCurrency newFavouriteCurrency,
        CancellationToken cancellationToken)
    {
        if (await _dbContext.FavouriteCurrencies.AnyAsync(f =>
                f.Name == newFavouriteCurrency.Name, cancellationToken))
            throw new NotUniqueFavouriteCurrency("Имя избранной валюты должно быть уникальным");

        if (await _dbContext.FavouriteCurrencies.AnyAsync(f =>
                f.Currency == newFavouriteCurrency.Currency &&
                f.BaseCurrency == newFavouriteCurrency.BaseCurrency, cancellationToken))
            throw new NotUniqueFavouriteCurrency(
                "Сочетание базовой валюты и основной валюты должно быть уникальным");

        await _dbContext.FavouriteCurrencies.AddAsync(newFavouriteCurrency, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    /// <exception cref="NotUniqueFavouriteCurrency">Выбрасывается, если имя не уникально.</exception>
    /// <exception cref="NotUniqueFavouriteCurrency">Выбрасывается, если сочетание базовой и основной валюты не уникально.</exception>
    public async Task EditFavouriteCurrencyAsync(string name, FavouriteCurrency editedFavouriteCurrency,
        CancellationToken cancellationToken)
    {
        var entity = await GetFavouriteCurrencyAsync(name, cancellationToken);

        entity.Currency = editedFavouriteCurrency.Currency;
        entity.BaseCurrency = editedFavouriteCurrency.BaseCurrency;
        entity.Name = editedFavouriteCurrency.Name;

        if (await _dbContext.FavouriteCurrencies.AnyAsync(f =>
                f != entity &&
                f.Name == editedFavouriteCurrency.Name, cancellationToken))
            throw new NotUniqueFavouriteCurrency("Имя избранной валюты должно быть уникальным");

        if (await _dbContext.FavouriteCurrencies.AnyAsync(f =>
                f != entity &&
                f.Currency == editedFavouriteCurrency.Currency &&
                f.BaseCurrency == editedFavouriteCurrency.BaseCurrency, cancellationToken))
            throw new NotUniqueFavouriteCurrency(
                "Сочетание базовой валюты и основной валюты избранного должно быть уникальным");

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    /// <exception cref="FavouriteCurrencyNotFoundException">Выбрасывается, если избранное <see cref="name"/> не найдено.</exception>
    public async Task DeleteFavouriteCurrencyAsync(string name, CancellationToken cancellationToken)
    {
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
}

/// <inheritdoc />
public class NotUniqueFavouriteCurrency : Exception
{
    /// <inheritdoc />
    public NotUniqueFavouriteCurrency(string? message) : base(message)
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