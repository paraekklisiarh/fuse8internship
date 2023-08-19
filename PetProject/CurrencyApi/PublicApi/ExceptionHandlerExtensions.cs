using Fuse8_ByteMinds.SummerSchool.PublicApi.Models;
using Fuse8_ByteMinds.SummerSchool.PublicApi.Services;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Fuse8_ByteMinds.SummerSchool.PublicApi;

/// <inheritdoc />
public class ExceptionHandlerExtensions : IAsyncExceptionFilter
{
    private readonly ILogger<ExceptionHandlerExtensions> _logger;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="logger"></param>
    public ExceptionHandlerExtensions(ILogger<ExceptionHandlerExtensions> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task OnExceptionAsync(ExceptionContext context)
    {
        ProblemDetails problemDetails;
        switch (context.Exception)
        {
            case RpcException { Status.StatusCode: StatusCode.Internal } exception:
                problemDetails = new ProblemDetails
                {
                    Title = "Произошла ошибка на сервере", Detail = exception.Status.Detail, Status = 500
                };
                _logger.LogError(exception, "Произошла ошибка GRPC-сервера");
                break;
            case RpcException {Status.StatusCode: StatusCode.ResourceExhausted}:
                problemDetails = new ProblemDetails
                {
                    Title = "Произошла ошибка на сервере",
                    Detail = "Закончились токены внешнего API",
                    Status = 429
                };
                _logger.LogCritical("Закончились токены внешнего API");
                break;
            case ApiSettingsAreNotSet exception:
                problemDetails = new ProblemDetails
                {
                    Title = "Настройки API не установлены",
                    Status = 404,
                    Detail = exception.Message,
                };
                break;
            case NotUniqueFavouriteCurrency exception:
                problemDetails = new ProblemDetails
                {
                    Title = "Ошибка создания/изменения избранного",
                    Status = (int?)StatusCode.AlreadyExists,
                    Detail = exception.Message
                };
                break;
            case FavouriteCurrencyNotFoundException exception:
                problemDetails = new ProblemDetails
                {
                    Title = "Не найдена избранная валюта",
                    Detail = exception.Message,
                    Status = 404
                };
                break;
            default:
                problemDetails = new ProblemDetails
                {
                    Title = "Внутренняя ошибка сервера",
                    Detail =
                        $"При обработке запроса {context.HttpContext.TraceIdentifier} произошла неожиданная ошибка.",
                    Status = 500
                };
                _logger.LogError(context.Exception, "Произошла неотловленная ошибка");
                break;
        }

        context.Result = new ObjectResult(problemDetails) { StatusCode = problemDetails.Status };
        context.ExceptionHandled = true;

        return Task.CompletedTask;
    }
}