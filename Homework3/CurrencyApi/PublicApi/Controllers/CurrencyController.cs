using System.ComponentModel.DataAnnotations;
using Fuse8_ByteMinds.SummerSchool.PublicApi.Models;
using Fuse8_ByteMinds.SummerSchool.PublicApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fuse8_ByteMinds.SummerSchool.PublicApi.Controllers;

/// <summary>
///     Методы для взаимодействия со внешним API
/// </summary>
[ApiController]
[Route("currency")]
public class CurrencyController : ControllerBase
{
    private readonly CurrencyApiSettings _apiConfiguration;
    private readonly ICurrencyService _currencyService;

    public CurrencyController(ICurrencyService currencyService, CurrencyApiSettings configuration)
    {
        _currencyService = currencyService;
        _apiConfiguration = configuration;
    }

    /// <summary>
    ///     Текущие настройки приложения
    /// </summary>
    /// <response code="200">Возвращает, если настройки успешно получены.</response>
    [Route("settings")]
    [HttpGet]
    public async Task<ActionResult<SettingsDto>> GetSettings()
    {
        var apiStatus = await _currencyService.RequestLimit();

        var response = new SettingsDto
        {
            defaultCurrency = _apiConfiguration.defaultCurrency,
            baseCurrency = _apiConfiguration.baseCurrency,
            currencyRoundCount = Convert.ToInt32(_apiConfiguration.currencyRoundCount),
            requestCount = apiStatus.used,
            requestLimit = apiStatus.total
        };

        return Ok(response);
    }

    /// <summary>
    ///     Получение курса валюты с кодом по умолчанию
    /// </summary>
    /// <response code="200">Возвращает, если значение успешно получено.</response>
    /// <response code="400">Возвращает, если значение по умолчанию не найдено</response>
    /// <response code="429">Возвращает, если токены API исчерпаны.</response>
    [HttpGet]
    public async Task<ActionResult<Currency>> GetDefaultCurrency()
    {
        var response = await _currencyService.GetDefaultCurrency();
        return Ok(response);
    }

    /// <summary>
    ///     Получение курса валюты на указанную дату
    /// </summary>
    /// <param name="currencyCode">Код валюты</param>
    /// <param name="date">Дата курса</param>
    /// <response code="200">Возвращает, если значение успешно получено.</response>
    /// <response code="404">Возвращает, если значение <see cref="currencyCode" /> не найдено</response>
    /// <response code="429">Возвращает, если токены API исчерпаны.</response>
    [HttpGet]
    [Route("{currencyCode:regex([[A-Z]]{{3}})}/{date}")]
    public async Task<ActionResult<CurrencyOnDateDto>> GetCurrency(string currencyCode,
        // Пробовал указать regex в route, но что-то сломалось
        [FromRoute] [RegularExpression(@"^\d{4}-\d{2}-\d{2}$")]
        string date)
    {
        var apiDto = await _currencyService.GetCurrencyOnDate(currencyCode, date);

        var currencyOnDateDto = new CurrencyOnDateDto
            { date = date, code = apiDto.code, value = apiDto.value };
        return Ok(currencyOnDateDto);
    }

    /// <summary>
    ///     Получение курса валюты по коду валюты
    /// </summary>
    /// <param name="currencyCode">Код валюты</param>
    /// <response code="200">Возвращает, если значение успешно получено.</response>
    /// <response code="404">Возвращает, если значение <see cref="currencyCode" /> не найдено</response>
    /// <response code="429">Возвращает, если токены API исчерпаны.</response>
    [HttpGet]
    [Route("{currencyCode:regex([[A-Z]]{{3}})}")]
    public async Task<ActionResult<Currency>> GetCurrencyByCode([FromRoute] string currencyCode)
    {
        var currency = await _currencyService.GetCurrency(currencyCode);

        return Ok(currency);
    }
}