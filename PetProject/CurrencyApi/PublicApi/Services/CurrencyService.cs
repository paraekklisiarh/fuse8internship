using System.Globalization;
using CurrencyApi;
using Fuse8_ByteMinds.SummerSchool.PublicApi.Models;
using Google.Protobuf.WellKnownTypes;
using Enum = System.Enum;

namespace Fuse8_ByteMinds.SummerSchool.PublicApi.Services;

/// <summary>
///     Курсы валют
/// </summary>
public interface ICurrencyService
{
    /// <summary>
    ///     Получение из внешнего API валюты по заданному коду
    /// </summary>
    /// <param name="currencyCode">Код валюты</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Объект <see cref="Currency" /></returns>
    public Task<Currency> GetCurrencyAsync(CurrencyTypeDTO currencyCode, CancellationToken cancellationToken);

    /// <summary>
    ///     Получение валюты с кодом по умолчанию
    /// </summary>
    /// <returns>Объект <see cref="Currency" /></returns>
    public Task<Currency> GetDefaultCurrencyAsync(CancellationToken cancellationToken);

    /// <summary>
    ///     Получение курса валюты на заданную дату
    /// </summary>
    /// <param name="currencyCode">Код валюты</param>
    /// <param name="date">Дата, курс на которую нужно получить, формата YYYY-MM-DD</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Объект <see cref="Currency" /></returns>
    public Task<Currency> GetCurrencyOnDateAsync(CurrencyTypeDTO currencyCode, DateOnly date,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Возвращает настройки grpc-сервера
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Настройки grpc-сервера</returns>
    public Task<Settings> GetCurrencyServerSettingsAsync(CancellationToken cancellationToken);
}

/// <inheritdoc />
public class CurrencyService : ICurrencyService
{
    private readonly GetCurrency.GetCurrencyClient _getCurrencyClient;
    private readonly IApiSettingsService _settings;

    /// <summary>
    ///     Курсы валют
    /// </summary>
    /// <param name="getCurrencyClient">gRPC клиент</param>
    /// <param name="settings">Настройки Api</param>
    public CurrencyService(GetCurrency.GetCurrencyClient getCurrencyClient, IApiSettingsService settings)
    {
        _getCurrencyClient = getCurrencyClient;
        _settings = settings;
    }

    /// <inheritdoc />
    public async Task<Currency> GetCurrencyAsync(CurrencyTypeDTO currencyCode, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var request = new Code
        {
            CurrencyType = currencyCode
        };

        var dto = await _getCurrencyClient.GetCurrencyAsync(request, cancellationToken: cancellationToken);

        var currency = await ParseCurrencyDto(dto, cancellationToken);

        return currency;
    }

    /// <inheritdoc />
    public async Task<Currency> GetDefaultCurrencyAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Enum.TryParse(await _settings.GetDefaultCurrencyAsync(cancellationToken), true,
            out CurrencyTypeDTO currencyType);

        var request = new Code
        {
            CurrencyType = currencyType
        };
        var dto = await _getCurrencyClient.GetCurrencyAsync(request, cancellationToken: cancellationToken);

        var currency = await ParseCurrencyDto(dto, cancellationToken);

        return currency;
    }

    /// <inheritdoc />
    public async Task<Currency> GetCurrencyOnDateAsync(CurrencyTypeDTO currencyCode, DateOnly date,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var request = new CodeAndDate
        {
            CurrencyType = currencyCode,
            Date = date.ToDateTime(new TimeOnly()).ToUniversalTime().ToTimestamp()
        };
        var dto = await _getCurrencyClient.GetCurrencyOnDateAsync(request, cancellationToken: cancellationToken);

        var currency = await ParseCurrencyDto(dto, cancellationToken);

        return currency;
    }

    /// <inheritdoc />
    public async Task<Settings> GetCurrencyServerSettingsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var settings = await _getCurrencyClient.GetSettingsAsync(new Empty(), cancellationToken: cancellationToken);

        return settings;
    }

    // ToDo: Этот метод дублирует такой же в FavouriteCurrencyService. Выделить в отдельный сервис?
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