﻿using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Fuse8_ByteMinds.SummerSchool.PublicApi;

public class ExceptionHandlerExtensions : IAsyncExceptionFilter
{
    private readonly ILogger<ExceptionHandlerExtensions> _logger;

    public ExceptionHandlerExtensions(ILogger<ExceptionHandlerExtensions> logger)
    {
        _logger = logger;
    }

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