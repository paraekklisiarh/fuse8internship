using System.ComponentModel.DataAnnotations;
using Fuse8_ByteMinds.SummerSchool.PublicApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fuse8_ByteMinds.SummerSchool.PublicApi.Controllers;

/// <summary>
/// Настройки Api
/// </summary>
[ApiController]
[Route("settings")]
public class SettingsController : ControllerBase
{
    private readonly IApiSettingsService _settingsService;
    
    public SettingsController(IApiSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    /// <summary>
    /// Установка валюты по умолчанию
    /// </summary>
    /// <param name="defaultCurrency">Новая валюта по умолчанию</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <response code="200">Возвращает, если настройки успешно применены.</response>
    /// <response code="400">Возвращает, если тип валюты неверен или неподдерживается.</response>
    [HttpPost]
    [Route("[action]/{defaultCurrency:length(3)}")]
    public async Task<IActionResult> DefaultCurrencySet([FromRoute] [RegularExpression("[A-Z]{3}")] string defaultCurrency, CancellationToken cancellationToken)
    {
        await _settingsService.DefaultCurrencySetAsync(defaultCurrency, cancellationToken);
        
        return Ok();
    }

    /// <summary>
    /// Установка знака после запятой до которого будет округлён курс валют.
    /// </summary>
    /// <param name="roundCount">Новый знак после запятой</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <response code="200">Возвращает, если настройки успешно применены.</response>
    [HttpPost]
    [Route("[action]/{roundCount:int:range(0,28)}")]
    public async Task<IActionResult> SetRoundCount([FromRoute] [Range(0,28)] int roundCount, CancellationToken cancellationToken)
    {
        await _settingsService.RoundCountSetAsync(roundCount, cancellationToken);

        return Ok();
    }
}