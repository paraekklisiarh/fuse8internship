using System.Text.Json.Serialization;
using Audit.Core;
using Audit.Http;
using Fuse8_ByteMinds.SummerSchool.PublicApi.Models;
using Fuse8_ByteMinds.SummerSchool.PublicApi.Services;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Extensions.Http;
using Serilog;

namespace Fuse8_ByteMinds.SummerSchool.PublicApi;

public class Startup
{
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers()
            // Добавляем глобальные настройки для преобразования Json
            .AddJsonOptions(
                options =>
                {
                    // Добавляем конвертер для енама
                    // По умолчанию енам преобразуется в цифровое значение
                    // Этим конвертером задаем перевод в строковое занчение
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });

        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(
                "v1",
                new OpenApiInfo());
            options.IncludeXmlComments(
                Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml"));
        });

        // Логирование входящих запросов
        services.AddHttpLogging(logging => { logging.LoggingFields = HttpLoggingFields.RequestPath; });

        // Пусть HttpClient`ами управляет умная часть приложения
        services.AddHttpClient<ICurrencyService, CurrencyService>()
            // Повторить запросы при неудаче
            .AddPolicyHandler(
                HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .WaitAndRetryAsync(3,
                        retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt) - 1)))
            // Добавляем логирование запросов
            .AddAuditHandler(audit => audit
                .IncludeRequestHeaders()
                .IncludeRequestBody()
                .IncludeResponseHeaders()
                .IncludeResponseBody()
                .IncludeContentHeaders()
            );

        services.AddSerilog(c => c
            .ReadFrom.Configuration(_configuration));

        Audit.Core.Configuration.Setup()
            .UseSerilog(config => config.Message(
                auditEvent => auditEvent.ToJson()));
        
        services.AddControllers(o =>
            o.Filters.Add<ExceptionHandlerExtensions>());

        // Регистрирую настройки внешнего API
        services.Configure<CurrencyApiSettings>(_configuration.GetSection("CurrencyAPI"));
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseHttpLogging();

        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseRouting()
            .UseEndpoints(endpoints => endpoints.MapControllers());
    }
}