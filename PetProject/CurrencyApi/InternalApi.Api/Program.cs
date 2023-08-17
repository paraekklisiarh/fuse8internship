using System.Text.Json.Serialization;
using Audit.Core;
using Audit.Http;
using InternalApi;
using InternalApi.Contracts;
using InternalApi.Dtos;
using InternalApi.Entities;
using InternalApi.Infrastructure;
using InternalApi.Services;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using ExceptionHandlerExtensions = InternalApi.ExceptionHandlerExtensions;

var builder = WebApplication.CreateBuilder();

//// Accessing IConfiguration and IWebHostEnvironment from the builder
IConfiguration configuration = builder.Configuration;
var env = builder.Environment;

//// Services
var services = builder.Services;

services.AddControllers()
    .AddJsonOptions(
        options =>
        {
            // Добавляем конвертер для енама
            // По умолчанию енам преобразуется в цифровое значение
            // Этим конвертером задаем перевод в строковое занчение
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

//// Db
services.RegisterDataBase(configuration);

//// Logging

// Serilog registration
services.AddSerilog(c => c
    .ReadFrom.Configuration(configuration));

// Логирование входящих запросов
services.AddHttpLogging(logging => { logging.LoggingFields = HttpLoggingFields.RequestPath; });

// Audit logging
Configuration.Setup()
    .UseSerilog(config => config.Message(
        auditEvent => auditEvent.ToJson()));

//// Exceptions
services.AddControllers(o =>
    o.Filters.Add<ExceptionHandlerExtensions>());

//// gRPC
services.AddGrpc()
    // Обработчик исключений в RPC-exceptions
    .AddServiceOptions<GetCurrencyService>(options => options.Interceptors.Add<ExceptionInterceptor>());
services.AddGrpcReflection();

//// HttpClient
// Пусть HttpClient`ами управляет умная часть приложения
services.AddHttpClient<ICurrencyApi, ApiService>()
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

//// Our services register

// CurrencyAPI cache services
services.AddTransient<ICachedCurrencyApi, CachedCurrencyApi>();
services.AddSingleton(typeof(RenewalDatesDictionary));

//// Global settings
// External API settings
services.Configure<CurrencyApiSettings>(configuration.GetSection("ExternalApis:CurrencyAPI"));

// Cache settings
services.Configure<CurrencyCacheSettings>(configuration.GetSection("Cache:CurrencyAPICache"));

//// Swagger
services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(
        "v1",
        new OpenApiInfo());
    options.IncludeXmlComments(
        Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml"));
});

var app = builder.Build();

// Накатываем миграции на базу данных
await MigrateDatabase(app);

app.UseHttpLogging();

if (env.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapGrpcReflectionService();
}

app.UseRouting()
    .UseEndpoints(endpoints =>
    {
        endpoints.MapGrpcService<GetCurrencyService>();
        endpoints.MapControllers();
    });

// RUN!
await app.RunAsync();

async Task MigrateDatabase(WebApplication webApplication)
{
    using var scope = webApplication.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (context == null)
        throw new Exception("Cannot initialize the database context");

    await context.Database.MigrateAsync();
}