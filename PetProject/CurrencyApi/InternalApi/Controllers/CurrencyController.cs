using InternalApi.Contracts;
using InternalApi.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TestGrpc;

namespace InternalApi.Controllers;

/// <summary>
///     Методы для взаимодействия со внешним API
/// </summary>
[ApiController]
[Route("currency")]
public class CurrencyController : ControllerBase
{
    private readonly ICachedCurrencyApi _cachedCurrencyApi;
    private readonly CurrencyCacheSettings _cacheSettings;
    private readonly ICurrencyApi _currencyApi;

    /// <summary>
    /// Конструктор контроллера
    /// </summary>
    /// <param name="cachedCurrencyApi">Сервис кеша для внешнего API</param>
    /// <param name="cacheSettings">Настройки</param>
    /// <param name="currencyApi">Сервис внешнего API</param>
    public CurrencyController(ICachedCurrencyApi cachedCurrencyApi, IOptions<CurrencyCacheSettings> cacheSettings,
        ICurrencyApi currencyApi)
    {
        _cachedCurrencyApi = cachedCurrencyApi;
        _cacheSettings = cacheSettings.Value;
        _currencyApi = currencyApi;
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
        var currency = await _cachedCurrencyApi.GetCurrentCurrencyAsync(currencyType, cancellationToken);
        return Ok(currency);
    }

    /// <summary>
    ///     Получение курса валюты на указанную дату
    /// </summary>
    /// <param name="currencyType">Тип валюты</param>
    /// <param name="date">Дата курса</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <response code="200">Возвращает, если значение успешно получено.</response>
    /// <response code="404">Возвращает, если значение <see cref="currencyCode" /> не найдено</response>
    /// <response code="429">Возвращает, если токены API исчерпаны.</response>
    [HttpGet]
    [Route("{currencyType}/{date}")]
    public async Task<ActionResult<Currency>> GetCurrencyOnDate(CurrencyType currencyType, DateOnly date,
        CancellationToken cancellationToken)
    {
        if (date > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return UnprocessableEntity("Неверная дата.");
        }
        var currency = await _cachedCurrencyApi.GetCurrencyOnDateAsync(currencyType, date, cancellationToken);
        return Ok(currency);
    }

    /// <summary>
    ///     Текущие настройки приложения
    /// </summary>
    /// <response code="200">Возвращает, если настройки успешно получены.</response>
    [HttpGet]
    [Route("settings")]
    public async Task<ActionResult<Settings>> GetSettings(CancellationToken cancellationToken)
    {
        var settings = new Settings
        {
            BaseCurrency = _cacheSettings.BaseCurrency.ToString(),
            NewRequestsAvailable = await _currencyApi.IsNewRequestsAvailable(cancellationToken)
        };
        return Ok(settings);
    }

    /// <summary>
    /// Проверить что API работает
    /// </summary>
    /// <param name="checkExternalApi">Необходимо проверить работоспособность внешнего API.
    /// Если FALSE или NULL - проверяется работоспособность только текущего API</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <response code="200">
    /// Возвращает если удалось получить доступ к API
    /// </response>
    /// <response code="400">
    /// Возвращает если удалось не удалось получить доступ к API
    /// </response>
    [HttpGet]
    [Route("healthcheck")]
    public async Task<ActionResult> GetHealthCheck(bool? checkExternalApi, CancellationToken cancellationToken)
    {
        if (checkExternalApi == null || !(bool)checkExternalApi)
            return Ok(new HealthCheckResult { CheckedOn = DateTimeOffset.UtcNow });
        
        try
        {
            await _currencyApi.IsNewRequestsAvailable(cancellationToken);
            return Ok(new HealthCheckResult
                { CheckedOn = DateTimeOffset.UtcNow, Status = HealthCheckResult.CheckStatus.Ok });
        }
        catch (Exception)
        {
            return BadRequest(new HealthCheckResult
                { CheckedOn = DateTimeOffset.UtcNow, Status = HealthCheckResult.CheckStatus.Failed });
        }
    }
}

/// <summary>
/// Результат проверки работоспособности API
/// </summary>
public record HealthCheckResult
{
    /// <summary>
    /// Дата проверки
    /// </summary>
    public DateTimeOffset CheckedOn { get; init; }

    /// <summary>
    /// Статус работоспособности API
    /// </summary>
    public CheckStatus Status { get; init; }

    /// <summary>
    /// Статус API
    /// </summary>
    public enum CheckStatus
    {
        /// <summary>
        /// API работает
        /// </summary>
        Ok = 1,

        /// <summary>
        /// Ошибка в работе API
        /// </summary>
        Failed = 2,
    }
}