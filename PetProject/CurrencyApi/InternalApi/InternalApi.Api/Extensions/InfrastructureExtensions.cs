using InternalApi.Configuration.Sources;
using InternalApi.Infrastructure.Data.CurrencyContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

namespace InternalApi.Extensions;

/// <summary>
///     Инфраструктурные методы расширения
/// </summary>
public static class InfrastructureExtensions
{
    /// <summary>
    ///     Регистрирует базу данных в контейнере зависимостей.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <param name="configuration">Настройки приложения</param>
    /// <returns>Коллекция сервисов с зарегистрированной базой данных.</returns>
    public static IServiceCollection RegisterDataBase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            var currentAssemblyName = typeof(AppDbContext).Assembly.FullName;
            var dbConnectionString = configuration.GetConnectionString("CurrencyApi");
            options.UseNpgsql(dbConnectionString,
                    b => b.MigrationsAssembly(currentAssemblyName)
                        .MigrationsHistoryTable(HistoryRepository.DefaultTableName, "cur").EnableRetryOnFailure())
                .UseSnakeCaseNamingConvention().UseAllCheckConstraints();
        });
        return services;
    }

    /// <summary>
    ///     Добавляет источник конфигурации к ConfigurationBuilder.
    /// </summary>
    /// <param name="builder">Объект <see cref="ConfigurationBuilder" />, к которому добавляется источник конфигурации.</param>
    /// <param name="optionsAction">Действие, которое настраивает параметры <see cref="DbContextOptionsBuilder" />.</param>
    /// <returns>Объект ConfigurationBuilder с добавленным источником конфигурации.</returns>
    public static IConfigurationBuilder AddEFConfiguration(this IConfigurationBuilder builder,
        Action<DbContextOptionsBuilder> optionsAction)
    {
        return builder.Add(new EFConfigurationSource(optionsAction));
    }
}