using System.Text.Json.Serialization;
using Audit.Core;
using Audit.Http;
using AutoMapper;
using CurrencyApi;
using Fuse8_ByteMinds.SummerSchool.PublicApi.Models;
using Fuse8_ByteMinds.SummerSchool.PublicApi.Services;
using Fuse8_ByteMinds.SummerSchool.PublicApi.Services.Mapper;
using Grpc.Health.V1;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.OpenApi.Models;
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

        services.AddDbContext<AppDbContext>((_, builder) =>
        {
            var currentAssemblyName = typeof(AppDbContext).Assembly.FullName;
            var dbConnectionString = _configuration.GetConnectionString("CurrencyApi");
            builder.UseNpgsql(
                    dbConnectionString,
                    b => b
                        .MigrationsAssembly(currentAssemblyName)
                        .MigrationsHistoryTable(HistoryRepository.DefaultTableName, "user")
                        .EnableRetryOnFailure())
                .UseSnakeCaseNamingConvention()
                .UseAllCheckConstraints();
        } );
        
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

        services.AddSerilog(c => c
            .ReadFrom.Configuration(_configuration));

        Configuration.Setup()
            .UseSerilog(config => config.Message(
                auditEvent => auditEvent.ToJson()));

        services.AddControllers(o =>
            o.Filters.Add<ExceptionHandlerExtensions>());


        // grpc client
        services.AddGrpcClient<GetCurrency.GetCurrencyClient>(o =>
            {
                var uriString = _configuration.GetValue<string>("GrpcUrl");
                if (uriString != null) o.Address = new Uri(uriString);
            })
            .AddAuditHandler(audit => audit
                .IncludeRequestHeaders()
                .IncludeRequestBody()
                .IncludeResponseHeaders()
                .IncludeResponseBody()
                .IncludeContentHeaders()
            );
        // grpc health check client
        services.AddGrpcClient<Health.HealthClient>(o =>
        {
            var uriString = _configuration.GetValue<string>("ExternalApis:CurrencyAPI:BaseUrl");
            if (uriString != null)
                o.Address = new Uri(uriString);
        })
            .AddAuditHandler(audit => audit
                .IncludeRequestHeaders()
                .IncludeRequestBody()
                .IncludeResponseHeaders()
                .IncludeResponseBody()
                .IncludeContentHeaders()
            );
        
        // Our services register
        services.AddTransient<ICurrencyService, CurrencyService>();
        services.AddTransient<IFavouriteCurrencyService, FavouriteCurrencyService>();
        // настройки внешнего API
        services.AddTransient<IApiSettingsService, ApiSettingsService>();
        
        //// AutoMapper
        services.AddAutoMapper(typeof(ApplicationProfile));
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