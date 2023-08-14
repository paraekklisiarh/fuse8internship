﻿using InternalApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace InternalApi;

/// <summary>
/// Глобальный обработчик исключений
/// </summary>
public class ExceptionHandlerExtensions : IExceptionFilter
{
    private readonly ILogger<ExceptionHandlerExtensions> _logger;

    /// <summary>
    /// Конструктор глобального обработчика исключений
    /// </summary>
    /// <param name="logger">Логгер</param>
    public ExceptionHandlerExtensions(ILogger<ExceptionHandlerExtensions> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public void OnException(ExceptionContext context)
    {
        ProblemDetails problemDetails;
        switch (context.Exception)
        {
            case ApiRequestLimitException:
                problemDetails = new ProblemDetails
                {
                    Title = "Запросы исчерпаны", Detail = $"На сервере закончились токены API.", Status = 429
                };

                _logger.LogError(context.Exception, "Закончились токены CurrencyAPI");
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
    }
}