using System.Text.Json;
using InternalApi.Configuration;
using InternalApi.Contracts;
using InternalApi.Dtos;
using InternalApi.Entities;
using Microsoft.Extensions.Options;

namespace InternalApi.Services;

/// <summary>
///     Сервис для взаимодействия с внешним Currency API
/// </summary>
public class ApiService : ICurrencyApi
{
    private readonly CurrencyApiSettings _apiSettings;
    private readonly HttpClient _httpClient;

    /// <summary>
    ///     Конструктор класса ApiService
    /// </summary>
    /// <param name="apiSettingsSnapshot">Настройки внешнего API</param>
    /// <param name="httpClient">Http клиент</param>
    public ApiService(IOptionsMonitor<CurrencyApiSettings> apiSettingsSnapshot,
        HttpClient httpClient)
    {
        _apiSettings = apiSettingsSnapshot.CurrentValue;
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(_apiSettings.BaseUrl);
    }

    /// <summary>
    ///     Получение актуального курса валют
    /// </summary>
    /// <param name="baseCurrency">Базовая валюта</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Массив <see cref="Currency" />, содержащий актуальный курс валют</returns>
    /// <exception cref="OperationCanceledException">Операция была отменена</exception>
    public async Task<RootCurrencyApiDto> GetAllCurrentCurrenciesAsync(string baseCurrency,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            // Формирование Http-запроса
            var url = "latest?" + "base_currency=" + baseCurrency.ToUpper();

            // Обработка данных из Http-запроса
            var responseApiBody = await HttpClientHelper(url, cancellationToken);
            var apiDto = JsonSerializer.Deserialize<RootCurrencyApiDto>(responseApiBody);

            return apiDto ??
                   throw new InvalidOperationException("Возникла неожиданная ошибка при получении курса валют");
        }

        throw new OperationCanceledException(cancellationToken);
    }

    /// <summary>
    ///     Получение курса валют на указанную дату
    /// </summary>
    /// <param name="baseCurrency">Базовая валюта</param>
    /// <param name="date">Дата курса валюты</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Объект <see cref="CurrenciesOnDateDto" />, содержащий курс валюты на указанную дату</returns>
    /// <exception cref="OperationCanceledException">Операция была отменена</exception>
    public async Task<RootCurrencyApiDto> GetAllCurrenciesOnDateAsync(string baseCurrency, DateOnly date,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (!await IsNewRequestsAvailable(cancellationToken))
                throw new ApiRequestLimitException("Токены внешнего API закончились");

            var url = "historical?" + "date=" + date + "&&base_currency=" + baseCurrency.ToUpper();

            // Обработка данных из Http-запроса
            var responseApiBody = await HttpClientHelper(url, cancellationToken);
            var apiDto = JsonSerializer.Deserialize<RootCurrencyApiDto>(responseApiBody);

            return apiDto ??
                   throw new InvalidOperationException("Возникла неожиданная ошибка при получении курса валют");
        }

        throw new OperationCanceledException(cancellationToken);
    }

    /// <summary>
    ///     Существуют ли доступные токены внешнего API?
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>true, если существуют доступные токены, иначе false</returns>
    /// <exception cref="OperationCanceledException">Операция была отменена</exception>
    public async Task<bool> IsNewRequestsAvailable(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            const string url = "status";
            // Формирование Http-запроса
            _httpClient.DefaultRequestHeaders.Add("apikey", _apiSettings.ApiKey);

            // Http request
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            _httpClient.DefaultRequestHeaders.Clear();

            // Обработка данных из Http-запроса
            var responseApiBody = await response.Content.ReadAsStringAsync(cancellationToken);

            var apiStatus = JsonSerializer.Deserialize<ApiStatusDto>(responseApiBody);

            return apiStatus != null && apiStatus.Quotas.Month.Remaining != 0;
        }

        throw new OperationCanceledException();
    }

    /// <summary>
    ///     Получение данных из внешнего API
    /// </summary>
    /// <param name="url">url</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Тело ответа в виде строки</returns>
    private async Task<string> HttpClientHelper(string url, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        // Проверка наличия свободных токенов
        if (!await IsNewRequestsAvailable(cancellationToken))
            throw new ApiRequestLimitException("Токены внешнего API закончились");

        // Формирование Http-запроса
        _httpClient.DefaultRequestHeaders.Add("apikey", _apiSettings.ApiKey);

        // Http request
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        _httpClient.DefaultRequestHeaders.Clear();

        // Обработка данных из Http-запроса
        var responseApiBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return responseApiBody;
    }
}

/// <inheritdoc />
public class ApiRequestLimitException : Exception
{
    /// <inheritdoc />
    public ApiRequestLimitException(string? message) : base(message)
    {
    }
}