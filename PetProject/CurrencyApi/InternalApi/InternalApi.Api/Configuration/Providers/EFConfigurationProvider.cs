using InternalApi.Entities;
using InternalApi.Infrastructure.Data.ConfigurationContext;
using Microsoft.EntityFrameworkCore;

namespace InternalApi.Configuration.Providers;

/// <summary>
///     Представляет провайдер конфигурации, который использует EF Core для доступа к данным из базы данных
/// </summary>
public class EFConfigurationProvider : ConfigurationProvider, IDisposable
{
    private readonly Timer _timer;

    /// <summary>
    ///     Инициализирует провайдер конфигурации
    /// </summary>
    /// <param name="optionsAction">
    ///     делегат optionsAction, который позволяет настроить опции контекста базы данных через объект
    ///     <see cref="DbContextOptionsBuilder" />
    /// </param>
    public EFConfigurationProvider(Action<DbContextOptionsBuilder> optionsAction)
    {
        OptionsAction = optionsAction;

        //ToDo Создай класс конфигурации провайдера со значениями таймера по умолчанию и передавай его из сорца.
        _timer = new Timer(_ => Load(),
            null,
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(5));

        // Накатываем на базу миграции
        var builder = new DbContextOptionsBuilder<ConfigurationDbContext>();
        OptionsAction(builder);
        using var dbContext = new ConfigurationDbContext(builder.Options);
        dbContext.Database.Migrate();
    }

    private Action<DbContextOptionsBuilder> OptionsAction { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        _timer.Change(Timeout.Infinite, 0);
        _timer.Dispose();
        Console.WriteLine("Dispose timer");
    }

    /// <summary>Sets a value for a given key.</summary>
    /// <param name="key">The configuration key to set.</param>
    /// <param name="value">The value to set.</param>
    public override void Set(string key, string? value)
    {
        if (value == null) return;

        var builder = new DbContextOptionsBuilder<ConfigurationDbContext>();
        OptionsAction(builder);
        using var dbContext = new ConfigurationDbContext(builder.Options);
        if (dbContext.ConfigurationEntities == null) throw new Exception("Null DB context");

        var item = dbContext.ConfigurationEntities.FirstOrDefault(i => i.Key == key);
        if (item == null) return;

        item.Value = value;
        dbContext.SaveChanges();

        Load();
        OnReload();
    }

    /// <summary>Loads (or reloads) the data for this provider.</summary>
    public override void Load()
    {
        Console.WriteLine("Получение конфигурации из базы данных");
        var builder = new DbContextOptionsBuilder<ConfigurationDbContext>();

        OptionsAction(builder);

        using var dbContext = new ConfigurationDbContext(builder.Options);
        if (dbContext.ConfigurationEntities == null) throw new Exception("Null DB context");

        dbContext.Database.EnsureCreated();

        Data = (!dbContext.ConfigurationEntities.Any()
            ? CreateAndSaveDefaultValues(dbContext)
            : dbContext.ConfigurationEntities.ToDictionary(c => c.Key, c => c.Value))!;
    }

    private static IDictionary<string, string> CreateAndSaveDefaultValues(ConfigurationDbContext dbContext)
    {
        Console.WriteLine("В базе данных не хранится конфигурация. Устанавливаю значения по умолчанию");

        // TODO Хранить в коде значения по умолчанию - моветон. Попробуй подтягивать из appsettings?
        var configValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Cache:CurrencyAPICache:CacheExpirationHours", "24" },
            { "Cache:CurrencyAPICache:BaseCurrency", "USD" }
        };

        if (dbContext.ConfigurationEntities == null) throw new Exception("Null DB context");

        dbContext.ConfigurationEntities.AddRange(configValues
            .Select(kvp => new ConfigurationEntity
            {
                Key = kvp.Key,
                Value = kvp.Value
            })
            .ToArray());
        dbContext.SaveChanges();

        Console.WriteLine($"Установлены значения по умолчанию {configValues}");

        return configValues;
    }
}