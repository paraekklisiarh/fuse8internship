using CurrencyApi;
using InternalApi.Entities;
using InternalApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace InternalApi.Controllers;

/// <summary>
///     Методы для взаимодействия со внешним API
/// </summary>
[ApiController]
[Route("currency")]
public class CurrencyController : ControllerBase
{
    private readonly ICurrencyService _currencyService;

    /// <summary>
    ///     Конструктор контроллера
    /// </summary>
    /// <param name="currencyService">Сервис управления настройками службы валют через REST-Api</param>
    public CurrencyController(ICurrencyService currencyService)
    {
        _currencyService = currencyService;
    }

    /// <summary>
    ///     Получение курса валюты с кодом по умолчанию
    /// </summary>
    /// <response code="200">Возвращает, если значение успешно получено.</response>
    /// <response code="400">Возвращает, если значение по умолчанию не найдено</response>
    /// <response code="429">Возвращает, если токены API исчерпаны.</response>
    [HttpGet]
    [Route("{currencyType}")]
    public async Task<ActionResult<Currency>> GetCurrency(CurrencyType currencyType,
        CancellationToken cancellationToken)
    {
        var dto = await _currencyService.GetCurrency(currencyType, cancellationToken);
        return Ok(dto);
    }

    /// <summary>
    ///     Получение курса валюты на указанную дату
    /// </summary>
    /// <param name="currencyType">Тип валюты</param>
    /// <param name="date">Дата курса</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <response code="200">Возвращает, если значение успешно получено.</response>
    /// <response code="404">Возвращает, если значение <see cref="currencyType" /> не найдено</response>
    /// <response code="429">Возвращает, если токены API исчерпаны.</response>
    [HttpGet]
    [Route("{currencyType}/{date}")]
    public async Task<ActionResult<Currency>> GetCurrencyOnDate(CurrencyType currencyType, DateOnly date,
        CancellationToken cancellationToken)
    {
        if (date.ToDateTime(new TimeOnly()).ToUniversalTime() > DateTime.UtcNow)
            return UnprocessableEntity("Неверная дата.");
        var dto = await _currencyService.GetCurrencyOnDate(currencyType, date, cancellationToken);
        return Ok(dto);
    }

    /// <summary>
    ///     Текущие настройки приложения
    /// </summary>
    /// <response code="200">Возвращает, если настройки успешно получены.</response>
    [HttpGet]
    [Route("settings")]
    public async Task<ActionResult<Settings>> GetSettings(CancellationToken cancellationToken)
    {
        var settings = await _currencyService.GetCurrencySettings(cancellationToken);
        return Ok(settings);
    }

    /// <summary>
    ///     Проверить что API работает
    /// </summary>
    /// <param name="checkExternalApi">
    ///     Необходимо проверить работоспособность внешнего API.
    ///     Если FALSE или NULL - проверяется работоспособность только текущего API
    /// </param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <response code="200">
    ///     Возвращает если удалось получить доступ к API
    /// </response>
    /// <response code="400">
    ///     Возвращает если удалось не удалось получить доступ к API
    /// </response>
    [HttpGet]
    [Route("healthcheck")]
    public async Task<ActionResult> GetHealthCheck(bool? checkExternalApi, CancellationToken cancellationToken)
    {
        if (checkExternalApi == null || !(bool)checkExternalApi)
            return Ok(new HealthCheckResult { CheckedOn = DateTimeOffset.UtcNow });
        return await _currencyService.IsExternalApiHealth(cancellationToken)
            ? Ok(new HealthCheckResult { CheckedOn = DateTimeOffset.UtcNow, Status = HealthCheckResult.CheckStatus.Ok })
            : BadRequest(new HealthCheckResult
            {
                CheckedOn = DateTimeOffset.UtcNow, Status = HealthCheckResult.CheckStatus.Failed
            });
    }

    /// <summary>
    ///     Заменить базовую валюту, относительно которой рассчитывается курс валют.
    /// </summary>
    /// <param name="newBaseCurrency">Новая базовая валюта</param>
    /// <response code="202">
    ///     Возвращает ID задачи, если начата смена базовой валюты.
    /// </response>
    [HttpPut]
    [Route("change-base-currency")]
    public async Task<IActionResult> CurrencyConversion(CurrencyType newBaseCurrency)
    {
        var taskId = await _currencyService.CurrencyConversion(newBaseCurrency);
        return Accepted(taskId);
    }
}

/// <summary>
///     Результат проверки работоспособности API
/// </summary>
public record HealthCheckResult
{
    /// <summary>
    ///     Дата проверки
    /// </summary>
    public DateTimeOffset CheckedOn { get; init; }

    /// <summary>
    ///     Статус работоспособности API
    /// </summary>
    public CheckStatus Status { get; init; }

    /// <summary>
    ///     Статус API
    /// </summary>
    public enum CheckStatus
    {
        /// <summary>
        ///     API работает
        /// </summary>
        Ok = 1,

        /// <summary>
        ///     Ошибка в работе API
        /// </summary>
        Failed = 2
    }
}