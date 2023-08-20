using System.Net.Sockets;
using Grpc.Health.V1;
using Microsoft.AspNetCore.Mvc;
using static Fuse8_ByteMinds.SummerSchool.PublicApi.Controllers.HealthCheckResult;

namespace Fuse8_ByteMinds.SummerSchool.PublicApi.Controllers;

/// <summary>
/// Методы для проверки работоспособности PublicApi
/// </summary>
[Route("healthcheck")]
public class HealthCheckController : ControllerBase
{
    private readonly Health.HealthClient _healthClient;

    /// <inheritdoc />
    public HealthCheckController(Health.HealthClient healthClient)
    {
        _healthClient = healthClient;
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
    public async Task<HealthCheckResult> Check(bool? checkExternalApi, CancellationToken cancellationToken)
    {
        if (checkExternalApi == null || !(bool)checkExternalApi)
            return new HealthCheckResult { Status = CheckStatus.Ok, CheckedOn = DateTimeOffset.Now };
        try
        {
            var response = await _healthClient.CheckAsync(new HealthCheckRequest(), cancellationToken: cancellationToken);
            return new HealthCheckResult
            {
                CheckedOn = DateTimeOffset.Now,
                Status = response.Status == (HealthCheckResponse.Types.ServingStatus)1 ? 
                    CheckStatus.Ok : 
                    CheckStatus.Failed
            };
        }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Unavailable) 
        {
            return new HealthCheckResult
            {
                CheckedOn = DateTimeOffset.Now,
                Status = CheckStatus.Failed
            };
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