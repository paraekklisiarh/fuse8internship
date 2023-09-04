using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using CurrencyApi;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using InternalApi.Configuration;
using InternalApi.Contracts;
using InternalApi.Dtos;
using InternalApi.Entities;
using Microsoft.Extensions.Options;
using Enum = System.Enum;

namespace InternalApi.Services;

/// <inheritdoc />
public class GrpcCurrencyService : GetCurrency.GetCurrencyBase
{
    private readonly ICachedCurrencyApi _currencyCacheService;
    private readonly CurrencyCacheSettings _currencyCacheSettings;
    private readonly ICurrencyApi _currencyApi;

    /// <inheritdoc />
    public GrpcCurrencyService(ICachedCurrencyApi currencyCache, IOptions<CurrencyCacheSettings> cacheConfiguration,
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
    /// <returns>Объект <see cref="CurrencyDTO" />, содержащий информацию о текущем курсе валюты.</returns>
    /// <exception cref="OperationCanceledException">Выбрасывается, если операция была отменена.</exception>
    public override async Task<CurrencyDTO> GetCurrency(Code request, ServerCallContext context)
    {
        while (!context.CancellationToken.IsCancellationRequested)
        {
            var couldParse = Enum.TryParse(request.CurrencyType.ToString(), true, out CurrencyType currencyType);
            if (!couldParse
                && !Enum.IsDefined(typeof(CurrencyType), currencyType))
                throw new ArgumentException($"Тип валюты{request.CurrencyType} не поддерживается");

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

            var couldParse = Enum.TryParse(request.CurrencyType.ToString(), true, out CurrencyType currencyType);
            if (!couldParse
                && !Enum.IsDefined(typeof(CurrencyType), currencyType))
                throw new ArgumentException($"Тип валюты{request.CurrencyType} не поддерживается");

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
        var couldParse = Enum.TryParse(currency.Code.ToString(), true, out CurrencyTypeDTO currencyType);
        if (!couldParse
            && !Enum.IsDefined(typeof(CurrencyTypeDTO), currencyType))
            throw new ArgumentException($"Тип валюты{currency.Code} не поддерживается");

        var parsedDto = new CurrencyDTO
        {
            CurrencyType = currencyType,
            Value = currency.Value.ToString(CultureInfo.InvariantCulture)
        };

        return parsedDto;
    }

    /// <summary>
    /// Получение текущего курса избранной валюты
    /// </summary>
    /// <param name="request">Запрос на получение текущего курса валюты</param>
    /// <param name="context">Контекст запроса</param>
    /// <returns>Текущий курс избранной валюты относительно переданной базовой</returns>
    public override async Task<CurrencyDTO> GetFavouriteCurrencyCurrent(FavouriteCurrency request,
        ServerCallContext context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        var couldParseBaseType = Enum.TryParse(request.BaseCurrencyType.ToString(), out CurrencyType baseCurrencyType);
        if (!couldParseBaseType && !Enum.IsDefined(typeof(CurrencyType), baseCurrencyType))
            throw new ArgumentException($"Тип валюты {request.BaseCurrencyType} не поддерживается");

        var couldParseCurrencyType = Enum.TryParse(request.CurrencyType.ToString(), out CurrencyType currencyType);
        if (!couldParseCurrencyType && !Enum.IsDefined(typeof(CurrencyType), currencyType))
            throw new ArgumentException($"Тип валюты {request.CurrencyType} не поддерживается");

        if (baseCurrencyType == _currencyCacheSettings.BaseCurrency)
        {
            var currency = await _currencyCacheService.GetCurrentCurrencyAsync(currencyType, context.CancellationToken);
            return ParseDto(currency);
        }

        var baseCurrency =
            await _currencyCacheService.GetCurrentCurrencyAsync(baseCurrencyType, context.CancellationToken);
        var targetCurrency =
            await _currencyCacheService.GetCurrentCurrencyAsync(currencyType, context.CancellationToken);

        return ParseDto(currency: targetCurrency with { Value = targetCurrency.Value / baseCurrency.Value });
    }

    /// <summary>
    /// Получение курса избранной валюты на указанную дату
    /// </summary>
    /// <param name="request">Запрос на получение курса валюты на указанную дату</param>
    /// <param name="context">Контекст запроса</param>
    /// <returns>Курс избранной валюты на указанную дату относительно переданной базовой</returns>
    public override async Task<CurrencyDTO> GetFavouriteCurrencyOnDate(FavouriteCurrencyOnDate request,
        ServerCallContext context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        var couldParseBaseType = Enum.TryParse(request.BaseCurrencyType.ToString(), out CurrencyType baseCurrencyType);
        if (!couldParseBaseType && !Enum.IsDefined(typeof(CurrencyType), baseCurrencyType))
            throw new ArgumentException($"Тип валюты{request.BaseCurrencyType} не поддерживается");

        var couldParseCurrencyType = Enum.TryParse(request.CurrencyType.ToString(), out CurrencyType currencyType);
        if (!couldParseCurrencyType && !Enum.IsDefined(typeof(CurrencyType), currencyType))
            throw new ArgumentException($"Тип валюты{request.CurrencyType} не поддерживается");

        // Парсинг даты в UTC
        var date = request.Date.ToDateTime().ToUniversalTime();
        if (date > DateTime.UtcNow) throw new ValidationException();

        var currencyDate = DateOnly.FromDateTime(date);

        if (baseCurrencyType == _currencyCacheSettings.BaseCurrency)
        {
            var currency =
                await _currencyCacheService.GetCurrencyOnDateAsync(currencyType, currencyDate,
                    context.CancellationToken);
            return ParseDto(currency);
        }

        var baseCurrency =
            await _currencyCacheService.GetCurrencyOnDateAsync(baseCurrencyType, currencyDate,
                context.CancellationToken);
        var targetCurrency =
            await _currencyCacheService.GetCurrencyOnDateAsync(currencyType, currencyDate, context.CancellationToken);

        return ParseDto(currency: targetCurrency with { Value = targetCurrency.Value / baseCurrency.Value });
    }
}