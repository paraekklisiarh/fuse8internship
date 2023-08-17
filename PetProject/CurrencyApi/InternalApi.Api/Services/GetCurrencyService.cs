using System.ComponentModel.DataAnnotations;
using System.Globalization;
using CurrencyApi;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using InternalApi.Contracts;
using InternalApi.Dtos;
using InternalApi.Entities;
using Microsoft.Extensions.Options;
using Enum = System.Enum;

namespace InternalApi.Services;

/// <inheritdoc />
public class GetCurrencyService : GetCurrency.GetCurrencyBase
{
    private readonly ICachedCurrencyApi _currencyCacheService;
    private readonly CurrencyCacheSettings _currencyCacheSettings;
    private readonly ICurrencyApi _currencyApi;

    /// <inheritdoc />
    public GetCurrencyService(ICachedCurrencyApi currencyCache, IOptions<CurrencyCacheSettings> cacheConfiguration,
        ICurrencyApi currencyApi)
    {
        _currencyCacheService = currencyCache;
        _currencyCacheSettings = cacheConfiguration.Value;
        _currencyApi = currencyApi;
    }

    /// <summary>
    ///     Получение текущего курса валюты по указанному коду
    /// </summary>
    /// <param name="request">Запрос на получение курса валюты</param>
    /// <param name="context">Контекст запроса</param>
    /// <returns>Объект <see cref="CurrencyDTO" />, содержащий информацию о курсе валюты на указанную дату.</returns>
    /// <exception cref="OperationCanceledException">Выбрасывается, если операция была отменена.</exception>
    public override async Task<CurrencyDTO> GetCurrency(Code request, ServerCallContext context)
    {
        while (!context.CancellationToken.IsCancellationRequested)
        {
            Enum.TryParse(request.CurrencyType.ToString(), true, out CurrencyType currencyType);

            // Получение DTO из кеша или API
            var currency = await _currencyCacheService
                .GetCurrentCurrencyAsync(currencyType, context.CancellationToken);

            return ParseDto(currency);
        }

        throw new OperationCanceledException(context.CancellationToken);
    }

    /// <summary>
    ///     Получение курса валюты по указанному коду на указанную дату
    /// </summary>
    /// <param name="request">Запрос на получение курса валюты</param>
    /// <param name="context">Контекст запроса</param>
    /// <returns>Объект <see cref="CurrencyDTO" />, содержащий информацию о курсе валюты на указанную дату.</returns>
    /// <exception cref="OperationCanceledException">Выбрасывается, если операция была отменена.</exception>
    public override async Task<CurrencyDTO> GetCurrencyOnDate(CodeAndDate request, ServerCallContext context)
    {
        while (!context.CancellationToken.IsCancellationRequested)
        {
            // Парсинг даты в UTC
            var date = request.Date.ToDateTime().ToUniversalTime();
            if (date > DateTime.UtcNow) throw new ValidationException();

            var currencyDate = DateOnly.FromDateTime(date);

            Enum.TryParse(request.CurrencyType.ToString(), out CurrencyType currencyType);

            // Получение DTO из кеша или API
            var currency = await _currencyCacheService.GetCurrencyOnDateAsync(
                currencyType, currencyDate, context.CancellationToken);

            return ParseDto(currency);
        }

        throw new OperationCanceledException(context.CancellationToken);
    }

    /// <summary>
    ///     Получение настроек
    /// </summary>
    /// <param name="request">Запрос на получение настоек</param>
    /// <param name="context">Контекст запроса</param>
    /// <returns>Объект <see cref="JsonFormatter.Settings" />, содержащий текущие настройки сервиса.</returns>
    /// <exception cref="OperationCanceledException">Выбрасывается, если операция была отменена.</exception>
    public override async Task<Settings> GetSettings(Empty request, ServerCallContext context)
    {
        while (!context.CancellationToken.IsCancellationRequested)
            return new Settings
            {
                BaseCurrency = _currencyCacheSettings.BaseCurrency.ToString(),
                NewRequestsAvailable = await _currencyApi.IsNewRequestsAvailable(context.CancellationToken)
            };

        throw new OperationCanceledException(context.CancellationToken);
    }

    /// <summary>
    ///     Маппинг курса валюты из кеша в DTO
    /// </summary>
    /// <param name="currency">Курс валюты из кеша</param>
    /// <returns>Объект <see cref="CurrencyDTO" />, содержащий курс валюты</returns>
    private static CurrencyDTO ParseDto(Currency currency)
    {
        Enum.TryParse(currency.Code.ToString(), true, out CurrencyTypeDTO currencyType);

        var parsedDto = new CurrencyDTO
        {
            CurrencyType = currencyType,
            Value = currency.Value.ToString(CultureInfo.InvariantCulture)
        };

        return parsedDto;
    }
}