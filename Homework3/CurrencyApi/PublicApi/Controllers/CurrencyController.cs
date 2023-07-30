﻿using System.ComponentModel.DataAnnotations;
using Fuse8_ByteMinds.SummerSchool.PublicApi.Models;
using Fuse8_ByteMinds.SummerSchool.PublicApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fuse8_ByteMinds.SummerSchool.PublicApi.Controllers;

[ApiController]
[Route("currency")]
public class CurrencyController : ControllerBase
{
    private readonly IConfigurationSection _apiConfiguration;
    private readonly ICurrencyService _currencyService;

    public CurrencyController(ICurrencyService currencyService, IConfiguration configuration)
    {
        _currencyService = currencyService;
        _apiConfiguration = configuration.GetSection("ExternalApis:CurrencyAPI");
    }

    /// <summary>
    /// Текущие настройки приложения
    /// </summary>
    /// <returns></returns>
    [Route("settings")]
    [HttpGet]
    public async Task<IActionResult> GetSettings()
    {
        var apiStatus = await _currencyService.RequestLimit();

        var response = new Dictionary<string, string>();

        // TODO: Как-то оптимизировать валидацию конфигурации, чтобы не захламлять код.
        response.Add("defaultCurrency",
            _apiConfiguration["defaultCurrency"] ?? throw new InvalidOperationException("Настройки API недоступны"));
        response.Add("baseCurrency",
            _apiConfiguration["baseCurrency"] ?? throw new InvalidOperationException("Настройки API недоступны"));
        response.Add("requestLimit", apiStatus.total.ToString());
        response.Add("requestCount", apiStatus.used.ToString());
        response.Add("currencyRoundCount",
            _apiConfiguration["currencyRoundCount"] ?? throw new InvalidOperationException("Настройки API недоступны"));

        return Ok(response);
    }

    /// <summary>
    /// Получение курса валюты с кодом по умолчанию
    /// </summary>
    /// <response code="200">Возвращает, если значение успешно получено.</response>
    /// <response code="400">Возвращает, если значение по умолчанию не найдено</response>
    [HttpGet]
    public async Task<IActionResult> GetDefaultCurrency()
    {
        var response = await _currencyService.GetDefaultCurrency();
        return Ok(response);
    }

    /// <summary>
    /// Получение курса валюты на указанную дату
    /// </summary>
    /// <param name="currencyCode">Код валюты</param>
    /// <param name="date">Дата курса</param>
    /// <response code="200">Возвращает, если значение успешно получено.</response>
    /// <response code="404">Возвращает, если значение <see cref="currencyCode"/> не найдено</response>
    /// <response code="429">Возвращает, если токены API исчерпаны.</response>
    [HttpGet]
    [Route("{currencyCode:maxlength(3):regex([[A-Z]]{{3}})}/{date}")]
    public async Task<IActionResult> GetCurrency(string currencyCode,
        // Пробовал указать regex в route, но что-то сломалось
        [FromRoute] [RegularExpression(@"^\d{4}-\d{2}-\d{2}$")] string date)
    {
        var apiDto = await _currencyService.GetCurrencyOnDate(currencyCode, date);
        
        var currencyOnDateDto = new CurrencyOnDateDto
            { date = date, code = apiDto.code, value = apiDto.value };
        return Ok(currencyOnDateDto);
    }

    /// <summary>
    /// Получение курса валюты на указанную дату
    /// </summary>
    /// <param name="currencyCode">Код валюты</param>
    /// <param name="date">Дата курса</param>
    /// <response code="200">Возвращает, если значение успешно получено.</response>
    /// <response code="404">Возвращает, если значение <see cref="currencyCode"/> не найдено</response>
    /// <response code="429">Возвращает, если токены API исчерпаны.</response>
    [HttpGet]
    [Route("{currencyCode}")]
    public async Task<IActionResult> GetCurrencyByCode([FromRoute] string currencyCode)
    {
        var currency = await _currencyService.GetCurrency(currencyCode);

        return Ok(currency);
    }
}