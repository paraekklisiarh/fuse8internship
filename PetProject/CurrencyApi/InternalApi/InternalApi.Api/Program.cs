using System.Net;
using System.Text.Json.Serialization;
using Audit.Core;
using Audit.Http;
using InternalApi;
using InternalApi.Configuration;
using InternalApi.Contracts;
using InternalApi.Entities;
using InternalApi.Extensions;
using InternalApi.Infrastructure.Data.ConfigurationContext;
using InternalApi.Infrastructure.Data.CurrencyContext;
using InternalApi.Services;
using InternalApi.Services.Cache;
using InternalApi.Services.CurrencyConversion;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using ExceptionHandlerExtensions = InternalApi.Extensions.ExceptionHandlerExtensions;
using ICurrencyService = InternalApi.Services.ICurrencyService;

var builder = WebApplication.CreateBuilder();

builder.Configuration.AddEFConfiguration(
    options =>
    {
        var currentAssemblyName = typeof(ConfigurationDbContext).Assembly.FullName;
        var dbConnectionString = builder.Configuration.GetConnectionString("CurrencyApi");

        options.UseNpgsql(
                dbConnectionString,
                b => b
                    .MigrationsAssembly(currentAssemblyName)
                    .MigrationsHistoryTable(HistoryRepository.DefaultTableName, "cur")
                    .EnableRetryOnFailure())
            .UseSnakeCaseNamingConvention()
            .UseAllCheckConstraints();
    });

//// Accessing IConfiguration and IWebHostEnvironment from the builder
IConfiguration configuration = builder.Configuration;

var env = builder.Environment;

//// Kestrel
builder.WebHost.UseKestrel((_, options) =>
{
    var grpcPort = configuration.GetValue<int>("GrpcPort");

    options.Listen(IPAddress.Any, 5050, cfg => { cfg.Protocols = HttpProtocols.Http1; });
    options.Listen(IPAddress.Any, grpcPort, cfg => { cfg.Protocols = HttpProtocols.Http2; });
});

//// Services
var services = builder.Services;

//// Db
services.RegisterDataBase(configuration);

services.AddControllers()
    .AddJsonOptions(
        options =>
        {
            // Добавляем конвертер для енама
            // По умолчанию енам преобразуется в цифровое значение
            // Этим конвертером задаем перевод в строковое занчение
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

//// Logging

// Serilog registration
services.AddSerilog(c => c
    .ReadFrom.Configuration(configuration));

// Логирование входящих запросов
services.AddHttpLogging(logging =>
{
    logging.LoggingFields = HttpLoggingFields.RequestPath | HttpLoggingFields.RequestMethod;
});

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
    .AddServiceOptions<GrpcCurrencyService>(options => options.Interceptors.Add<ExceptionInterceptor>());
services.AddGrpcReflection();
services.AddGrpcHealthChecks();

services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>()
    .AddUrlGroup(new Uri(configuration.GetValue<string>("ExternalApis:CurrencyAPI:BaseUrl") + "status"), "CurrencyApi",
        timeout: TimeSpan.FromMinutes(10), configureClient: (_, client) =>
        {
            client.DefaultRequestHeaders.Add("apikey",
                configuration.GetValue<string>("ExternalApis:CurrencyAPI:ApiKey"));
        });

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

// CurrencyAPI services
services.AddMemoryCache();
services.AddTransient<ICachedCurrencyApi, CachedCurrencyApi>();
services.AddSingleton(typeof(RenewalDatesDictionary));
services.AddTransient<ICurrencyService, CurrencyService>();

//// Global settings
// External API settings
services.Configure<CurrencyApiSettings>(configuration.GetSection("ExternalApis:CurrencyAPI"));

// Cache settings
services.Configure<CurrencyCacheSettings>(configuration.GetSection("Cache:CurrencyAPICache"));


//// Cache recalculation bg services
services.AddSingleton<IInternalQueue<CurrencyConversionTask>, InternalCurrencyConversionQueue>();
services.AddScoped<ICurrencyConversionService, CurrencyConversionService>();
services.AddHostedService<BackgroundCurrencyConversionService>();

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
await MigrateDatabases(app);

app.UseHttpLogging();


if (env.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapGrpcReflectionService();
}

// HealthCheck
app.UseHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "applications/json";
        var response = new HealthCheckResponse
        {
            Status = report.Status.ToString(),
            HealthChecks = report.Entries.Select(x => new IndividualHealthCheckResponse
            {
                Status = x.Key,
                Component = x.Value.Status.ToString(),
                Description = x.Value.Description
            }),
            HealthCheckDuration = report.TotalDuration
        };
        await context.Response.WriteAsJsonAsync(response);
    }
});

// gRPC port
app.UseWhen(
    c => c.Connection.LocalPort == configuration.GetValue<int>("GrpcPort"),
    grpcBuilder =>
    {
        grpcBuilder.UseRouting();
        grpcBuilder.UseEndpoints(e =>
        {
            e.MapGrpcService<GrpcCurrencyService>();
            e.MapGrpcHealthChecksService();
        });
    });

// REST
app.UseRouting();
app.MapControllers();

// RUN!
await app.RunAsync();

async Task MigrateDatabases(IHost webApplication)
{
    using var scope = webApplication.Services.CreateScope();
    List<DbContext> contexts = new()
    {
        scope.ServiceProvider.GetRequiredService<AppDbContext>(),
    };

    foreach (var context in contexts)
    {
        await context.Database.MigrateAsync();
    }
}