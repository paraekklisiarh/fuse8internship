using Grpc.Core;
using Grpc.Core.Interceptors;
using InternalApi.Services;

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
        catch (ApiRequestLimitException)
        {
            _logger.LogCritical("Закончились токены CurrencyAPI");

            throw new RpcException(new Status (StatusCode.ResourceExhausted, "Закончились токены внешнего API"));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Произошла неотловленная ошибка");
            throw new RpcException(new Status(StatusCode.Internal, "Произошла внутренняя ошибка"));
        }
    }
}