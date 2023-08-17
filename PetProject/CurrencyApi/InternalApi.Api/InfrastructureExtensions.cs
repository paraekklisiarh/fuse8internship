using InternalApi.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

namespace InternalApi;

/// <summary>
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
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            var currentAssemblyName = typeof(AppDbContext).Assembly.FullName;
            var dbConnectionString = configuration.GetConnectionString("InternalApi");
            options.UseNpgsql(
                dbConnectionString,
                b => b
                    .MigrationsAssembly(currentAssemblyName)
                    .MigrationsHistoryTable(HistoryRepository.DefaultTableName, "cur")
                    .EnableRetryOnFailure())
                .UseSnakeCaseNamingConvention();
        });

        return services;
    }
}