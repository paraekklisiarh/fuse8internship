﻿using CurrencyApi;
using Fuse8_ByteMinds.SummerSchool.PublicApi.Dtos;
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
    private readonly ICurrencyService _currencyService;

    /// <inheritdoc />
    public CurrencyController(ICurrencyService currencyService)
    {
        _currencyService = currencyService;
    }

    /// <summary>
    ///     Получение курса валюты с кодом по умолчанию
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <response code="200">Возвращает, если значение успешно получено.</response>
    /// <response code="400">Возвращает, если значение по умолчанию не найдено</response>
    /// <response code="429">Возвращает, если токены API исчерпаны.</response>
    [HttpGet]
    public async Task<ActionResult<Currency>> GetDefaultCurrency(CancellationToken cancellationToken)
    {
        var response = await _currencyService.GetDefaultCurrencyAsync(cancellationToken);
        return Ok(response);
    }

    /// <summary>
    ///     Получение курса валюты на указанную дату
    /// </summary>
    /// <param name="currencyCode">Код валюты</param>
    /// <param name="date">Дата курса</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <response code="200">Возвращает, если значение успешно получено.</response>
    /// <response code="404">Возвращает, если значение <see cref="currencyCode" /> не найдено</response>
    /// <response code="429">Возвращает, если токены API исчерпаны.</response>
    [HttpGet]
    [Route("{currencyCode}/{date}")]
    public async Task<ActionResult<CurrencyOnDateDto>> GetCurrency(
        [FromRoute] DateOnly date, [FromRoute] CurrencyTypeDTO currencyCode, CancellationToken cancellationToken)
    {
        var apiDto = await _currencyService.GetCurrencyOnDateAsync(currencyCode, date, cancellationToken);

        var currencyOnDateDto = new CurrencyOnDateDto
            { Date = date, Code = apiDto.Code, Value = apiDto.Value };
        return Ok(currencyOnDateDto);
    }

    /// <summary>
    ///     Получение курса валюты по коду валюты
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <param name="currencyCode">Код валюты</param>
    /// <response code="200">Возвращает, если значение успешно получено.</response>
    /// <response code="404">Возвращает, если значение <see cref="currencyCode" /> не найдено</response>
    /// <response code="429">Возвращает, если токены API исчерпаны.</response>
    [HttpGet]
    [Route("{currencyCode}")]
    public async Task<ActionResult<Currency>> GetCurrencyByCode([FromRoute] CurrencyTypeDTO currencyCode,
        CancellationToken cancellationToken)
    {
        var currency = await _currencyService.GetCurrencyAsync(currencyCode, cancellationToken);

        return Ok(currency);
    }
}