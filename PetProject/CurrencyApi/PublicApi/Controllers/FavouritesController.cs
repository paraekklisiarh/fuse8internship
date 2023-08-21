using Fuse8_ByteMinds.SummerSchool.PublicApi.Dtos;
using Fuse8_ByteMinds.SummerSchool.PublicApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fuse8_ByteMinds.SummerSchool.PublicApi.Controllers;

/// <summary>
///     Управление избранными валютами
/// </summary>
[ApiController]
[Route("favourites/[action]")]
public class FavouritesController : ControllerBase
{
    private readonly FavouriteCurrencyService _favouriteService;

    /// <summary>
    ///     Управление избранными валютами
    /// </summary>
    /// <param name="favouriteService">Сервис управления избранным</param>
    public FavouritesController(FavouriteCurrencyService favouriteService)
    {
        _favouriteService = favouriteService;
    }

    /// <summary>
    ///     Получить список всех Избранных
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    [HttpGet]
    public async Task<IActionResult> GetFavourites(CancellationToken cancellationToken)
    {
        var response = await _favouriteService.GetFavouritesCurrenciesAsync(cancellationToken);

        return Ok(response);
    }

    /// <summary>
    ///     Получить Избранное по его названию
    /// </summary>
    /// <param name="name">Название элемента избранного</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <response code="200">Возвращает, если значение успешно получено.</response>
    /// <response code="404">Возвращает, если сущность <see cref="name" /> не найдена.</response>
    [HttpGet]
    [Route("{name}")]
    public async Task<IActionResult> GetFavourite(string name, CancellationToken cancellationToken)
    {
        var favouriteCurrency = await _favouriteService.GetFavouriteCurrencyAsync(name, cancellationToken);

        return Ok(favouriteCurrency);
    }

    /// <summary>
    ///     Добавить новое Избранное
    /// </summary>
    /// <param name="favouriteCurrencyDto">Новый элемент избранного</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <response code="201">Возвращает, если сущность успешно создана.</response>
    [HttpPost]
    [Route("")]
    public async Task<IActionResult> CreateFavourite(FavouriteCurrencyDto favouriteCurrencyDto,
        CancellationToken cancellationToken)
    {
        await _favouriteService.AddFavouriteCurrencyAsync(favouriteCurrencyDto, cancellationToken);

        var location = Url.Action(nameof(GetFavourite), favouriteCurrencyDto.Name);
        return Created(location!, favouriteCurrencyDto);
    }

    /// <summary>
    ///     Изменить Избранное по его названию
    /// </summary>
    /// <param name="name">Название элемента избранного</param>
    /// <param name="favouriteCurrencyDto">Обновленный элемент избранного</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <response code="204">Возвращает, если сущность успешно обновлена.</response>
    /// <response code="404">Возвращает, если сущность <see cref="name" /> не найдена.</response>
    /// 409
    /// <response code="409">Возвращает, если сущность <see cref="favouriteCurrencyDto" /> не уникальна.</response>
    [HttpPut]
    [Route("{name}")]
    public async Task<IActionResult> UpdateFavourite(string name, FavouriteCurrencyDto favouriteCurrencyDto,
        CancellationToken cancellationToken)
    {
        await _favouriteService.EditFavouriteCurrencyAsync(name, favouriteCurrencyDto, cancellationToken);

        return NoContent();
    }

    /// <summary>
    ///     Удалить избранное по его названию
    /// </summary>
    /// <param name="name">Название удаляемого элемента избранного</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <response code="200">Возвращает, если сущность успешно удалена.</response>
    /// <response code="404">Возвращает, если сущность <see cref="name" /> не найдена.</response>
    [HttpDelete]
    [Route("{name}")]
    public async Task<IActionResult> DeleteFavourite(string name, CancellationToken cancellationToken)
    {
        await _favouriteService.DeleteFavouriteCurrencyAsync(name, cancellationToken);

        return Ok();
    }


    /// <summary>
    ///     Получение текущего курса Избранного по его названию
    /// </summary>
    /// <param name="name">Название элемента избранного</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <response code="200">Возвращает, если значение успешно получено.</response>
    /// <response code="404">Возвращает, если сущность <see cref="name" /> не найдена.</response>
    /// <response code="429">Возвращает, если токены API исчерпаны.</response>
    [HttpGet]
    [Route("{name}")]
    public async Task<IActionResult> GetFavouriteCurrencyCurrent(string name,
        CancellationToken cancellationToken)
    {
        var result = await _favouriteService.GetFavouriteCurrencyCurrentAsync(name, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    ///     Получение курса Избранного по его названию на конкретную дату
    /// </summary>
    /// <param name="name">Название элемента избранного</param>
    /// <param name="targetDate">Дата курса валюты</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <response code="200">Возвращает, если значение успешно получено.</response>
    /// <response code="404">Возвращает, если сущность <see cref="name" /> не найдена.</response>
    /// <response code="429">Возвращает, если токены API исчерпаны.</response>
    [HttpGet]
    [Route("{name}/{targetDate}")]
    public async Task<IActionResult> GetFavouriteCurrencyOnDate([FromRoute] string name, [FromRoute] DateOnly targetDate,
        CancellationToken cancellationToken)
    {
        var result = await _favouriteService.GetFavouriteCurrencyOnDateAsync(name, targetDate, cancellationToken);

        return Ok(result);
    }
}