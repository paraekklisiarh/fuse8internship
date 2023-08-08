// TODO: Сервис разросся. Выдели взаимодействие с сервером и валидацию ответов в отдельный класс.

using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Fuse8_ByteMinds.SummerSchool.PublicApi.Dtos;
using Fuse8_ByteMinds.SummerSchool.PublicApi.Models;
using Microsoft.Extensions.Options;

namespace Fuse8_ByteMinds.SummerSchool.PublicApi.Services;

public interface ICurrencyService
{
    public Task<Currency> GetCurrency(string currencyCode);

    public Task<Currency> GetDefaultCurrency();

    public Task<Currency> GetCurrencyOnDate(string code, string date);

    public Task<SettingsDto> GetSettings();
}

public class CurrencyService : ICurrencyService
{
    private readonly CurrencyApiSettings _apiConfiguration;

    private static HttpClient _httpClient = null!;

    public CurrencyService(IOptions<CurrencyApiSettings> configuration, HttpClient httpClient)
    {
        _apiConfiguration = configuration.Value;
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(_apiConfiguration.BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("apikey", _apiConfiguration.ApiKey);
    }

    /// <summary>
    ///     Получение из внешнего API валюты по заданному коду
    /// </summary>
    /// <param name="currencyCode">Код валюты</param>
    /// <returns>Объект <see cref="Currency" /></returns>
    public async Task<Currency> GetCurrency(string currencyCode)
    {
        await IsRequestLimitNotZero();

        _httpClient.DefaultRequestHeaders.Add("base_currency", _apiConfiguration.BaseCurrency);

        using var response = await _httpClient.GetAsync("latest?currencies=" + currencyCode);
        if (response.StatusCode is HttpStatusCode.UnprocessableEntity)
            throw new CurrencyNotFoundException("Валюта с таким кодом не найдена.");

        response.EnsureSuccessStatusCode();

        var currency = await ParseCurrencyFromApiResponse(response, currencyCode);
        return currency;
    }

    /// <summary>
    ///     Получение валюты с кодом по умолчанию
    /// </summary>
    /// <returns>Объект <see cref="Currency" /></returns>
    public async Task<Currency> GetDefaultCurrency()
    {
        await IsRequestLimitNotZero();
        var currencyCode = _apiConfiguration.DefaultCurrency;

        _httpClient.DefaultRequestHeaders.Add("base_currency", currencyCode);

        using var response = await _httpClient.GetAsync("latest?currencies=" + currencyCode);
        if (response.StatusCode is HttpStatusCode.UnprocessableEntity)
            throw new CurrencyNotFoundException("Валюта с таким кодом не найдена.");

        response.EnsureSuccessStatusCode();

        var currency = await ParseCurrencyFromApiResponse(response, currencyCode!);

        return currency;
    }

    /// <summary>
    ///     Получение курса валюты на заданную дату
    /// </summary>
    /// <param name="currencyCode">Код валюты</param>
    /// <param name="date">Дата, курс на которую нужно получить, формата YYYY-MM-DD</param>
    /// <returns>Объект <see cref="Currency" /></returns>
    public async Task<Currency> GetCurrencyOnDate(string currencyCode, string date)
    {
        await IsRequestLimitNotZero();

        using var response = await _httpClient.GetAsync(
            "latest?currencies=" + currencyCode + "&&date=" + date);
        if (response.StatusCode is HttpStatusCode.UnprocessableEntity)
            throw new CurrencyNotFoundException("Валюта с таким кодом не найдена.");

        response.EnsureSuccessStatusCode();

        var currency = await ParseCurrencyFromApiResponse(response, currencyCode);
        return currency;
    }

    /// <summary>
    ///     Получение текущих настроек API
    /// </summary>
    /// <returns>Объект <see cref="SettingsDto" />, содержащий актуальные настройки API</returns>
    public async Task<SettingsDto> GetSettings()
    {
        var apiStatus = await RequestApiStatus();

        var dto = new SettingsDto
        {
            DefaultCurrency = _apiConfiguration.DefaultCurrency,
            BaseCurrency = _apiConfiguration.BaseCurrency,
            CurrencyRoundCount = Convert.ToInt32(_apiConfiguration.CurrencyRoundCount),
            RequestCount = apiStatus.Quotas.Month.Used,
            RequestLimit = apiStatus.Quotas.Month.Total
        };

        return dto;
    }

    /// <summary>
    ///     Получить лимиты запросов API
    /// </summary>
    /// <returns>
    ///     total: общее количество доступных запросов внешнего API;
    ///     user: количество использованных запросов;
    /// </returns>
    private async Task<ApiStatusDto> RequestApiStatus()
    {
        using var response = await _httpClient.GetAsync("status");
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        
        var apiStatus = JsonSerializer.Deserialize<ApiStatusDto>(responseBody);

        return apiStatus ?? throw new InvalidOperationException();
    }

    /// <summary>
    ///     Проверяет, равно ли количество доступных токенов нулю. Если равно - исключение.
    /// </summary>
    /// <exception cref="ApiRequestLimitException">Количество доступных токенов равно нулю.</exception>
    private async Task IsRequestLimitNotZero()
    {
        var apiStatus = await RequestApiStatus();

        if (apiStatus.Quotas.Month.Remaining <= apiStatus.Quotas.Month.Used)
        {
            throw new ApiRequestLimitException("Свободные токены CurrencyAPI закончились");
        }
    }

    /// <summary>
    ///     Округление курса валюты до знака из конфигурации
    /// </summary>
    /// <param name="value">Округляемое значение</param>
    /// <returns>Округленное до знака после запятой из конфигурации значение</returns>
    private decimal Rounding(decimal value)
    {
        return Math.Round(value, Convert.ToInt32(_apiConfiguration.CurrencyRoundCount));
    }

    /// <summary>
    ///     Парсинг <see cref="Currency" /> из <see cref="HttpResponseMessage" />
    /// </summary>
    /// <param name="response">
    ///     <see cref="HttpResponseMessage" />
    /// </param>
    /// <param name="code">Код валюты</param>
    /// <returns>Объект <see cref="Currency" /></returns>
    /// <exception cref="BadHttpRequestException">
    ///     Объект <see cref="Currency" /> не найден, получены неверные данные из вызывающего метода
    /// </exception>
    private async Task<Currency> ParseCurrencyFromApiResponse(HttpResponseMessage response, string code)
    {
        var responseApiBody = await response.Content.ReadAsStringAsync();

        var apiDto = JsonSerializer.Deserialize<CurrencyApiDto>(responseApiBody);

        var currency = apiDto!.Data[code];

        currency.Value = Rounding(currency.Value);

        return currency;
    }
}

internal class CurrencyNotFoundException : Exception
{
    public CurrencyNotFoundException(string message) : base(message)
    {
    }
}

public class ApiRequestLimitException : Exception
{
    public ApiRequestLimitException(string? message) : base(message)
    {
    }
}