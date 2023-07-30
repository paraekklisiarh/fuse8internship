using System.Net;
using Fuse8_ByteMinds.SummerSchool.PublicApi.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Serilog;

namespace Fuse8_ByteMinds.SummerSchool.PublicApi;

public class ExceptionHandlerExtensions : IExceptionFilter
{
    private ILogger<ExceptionHandlerExtensions> _logger;
    public ExceptionHandlerExtensions(ILogger<ExceptionHandlerExtensions> logger)
    {
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        ProblemDetails problemDetails;
        switch (context.Exception)
        {
            case CurrencyNotFoundException:
                problemDetails = new ProblemDetails
                {
                    Title = "Валюта не найдена", Detail = $"Валюты с таким кодом не существует.", Status = 404
                };
                break;
            case ApiRequestLimitException:
                problemDetails = new ProblemDetails
                {
                    Title = "Запросы исчерпаны", Detail = $"На сервере закончились токены API.", Status = 429
                };
                
                Log.Error(context.Exception, "Закончились токены CurrencyAPI");
                break;
            default:
                problemDetails = new ProblemDetails
                {
                    Title = "Внутренняя ошибка сервера",
                    Detail =
                        $"При обработке запроса {context.HttpContext.TraceIdentifier} произошла неожиданная ошибка.",
                    Status = 500
                };
                Log.Error(context.Exception, "Произошла неотловленная ошибка");
                break;
        }

        context.Result = new ObjectResult(problemDetails){StatusCode = problemDetails.Status};
        context.ExceptionHandled = true;
    }
}