using Grpc.Core;
using Grpc.Core.Interceptors;
using InternalApi.Services;
using InternalApi.Services.Cache;

namespace InternalApi;

/// <summary>
///     Глобальный обработчик исключений для grpc-запросов
/// </summary>
public class ExceptionInterceptor : Interceptor
{
    private readonly ILogger<ExceptionInterceptor> _logger;


    ///
    public ExceptionInterceptor(ILogger<ExceptionInterceptor> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(request, context);
        }
        catch (OperationCanceledException exception)
        {
            _logger.LogInformation("Операция {Trace} была отменена", exception.StackTrace);
            throw new RpcException(new Status(StatusCode.Cancelled, "Операция отменена"));
        }
        catch (ApiRequestLimitException exception)
        {
            _logger.LogCritical(exception, "Закончились токены внешнего API");

            throw new RpcException(new Status(StatusCode.ResourceExhausted, "Закончились токены внешнего API"));
        }
        catch (CacheEntityNotFoundException exception)
        {
            _logger.LogCritical(exception, "Кеш повреждён");

            throw new RpcException(new Status(StatusCode.Internal, "Произошла внутренняя ошибка"));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Произошла неотловленная ошибка");
            throw new RpcException(new Status(StatusCode.Internal, "Произошла внутренняя ошибка"));
        }
    }
}