using InternalApi.Infrastructure;
using InternalApi.Infrastructure.Data.ConfigurationContext;
using InternalApi.Infrastructure.Data.CurrencyContext;
using Microsoft.EntityFrameworkCore;

namespace InternalApi.Tests;

public class TestAppDbContextDatabaseFixture
{
    private const string ConnectionString = "Host=localhost; Database=currency_api_tests; Username=postgres; Password=admin";

    private static readonly object _lock = new();
    private static bool _dbInitialized;

    public TestAppDbContextDatabaseFixture()
    {
        using var context = CreateContext();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        context.SaveChanges();
        Cleanup();
    }

    public AppDbContext CreateContext() => new AppDbContext(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .EnableThreadSafetyChecks()
            .Options);
    
    public void Cleanup()
    {
        using var context = CreateContext();
        context.Currencies.RemoveRange(context.Currencies);
        context.SaveChanges();
    }
}

[CollectionDefinition("TransactionalTests")]
public class TransactionalTestsCollection : ICollectionFixture<TestAppDbContextDatabaseFixture>
{
}

public class TestOptionsDbContextFixture
{
    private const string ConnectionString = "Host=localhost; Database=currency_api_tests; Username=postgres; Password=admin";

    private static readonly object _lock = new();
    private static bool _dbInitialized;

    public TestOptionsDbContextFixture()
    {
        using var context = CreateContext();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        context.SaveChanges();
        Cleanup();
    }

    public ConfigurationDbContext CreateContext() => new ConfigurationDbContext(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .EnableThreadSafetyChecks()
            .Options);
    
    public void Cleanup()
    {
        using var context = CreateContext();
        context.ConfigurationEntities.RemoveRange(context.ConfigurationEntities);
        context.SaveChanges();
    }
}
[CollectionDefinition("ConfigurationFromDbTests")]
public class ConfigurationsFromDbTests : ICollectionFixture<TestOptionsDbContextFixture>
{
}