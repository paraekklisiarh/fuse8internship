using System.Net;
using Fuse8_ByteMinds.SummerSchool.PublicApi.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Fuse8_ByteMinds.SummerSchool.PublicApi;

public class ExceptionHandlerExtensions : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var problemDetails = context.Exception switch
        {
            CurrencyNotFoundException => new ProblemDetails
            {
                Title = "Валюта не найдена", Detail = $"Валюты с таким кодом не существует.", Status = 404
            },
            ApiRequestLimitException => new ProblemDetails
            {
                Title = "Запросы исчерпаны", Detail = $"На сервере закончились токены API.", Status = 429
            },
            _ => new ProblemDetails
            {
                Title = "Внутренняя ошибка сервера",
                Detail = $"При обработке запроса {context.HttpContext.TraceIdentifier} произошла неожиданная ошибка.",
                Status = 500
            }
        };
        
        context.Result = new ObjectResult(problemDetails){StatusCode = problemDetails.Status};
        context.ExceptionHandled = true;
    }
}