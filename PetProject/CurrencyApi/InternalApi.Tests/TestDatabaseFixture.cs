using InternalApi.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InternalApi.Tests;

public class TestDatabaseFixture
{
    private const string ConnectionString = "Host=localhost; Database=currency_api_tests; Username=postgres; Password=admin";

    private static readonly object _lock = new();
    private static bool _dbInitialized;

    public TestDatabaseFixture()
    {
        using var context = CreateContext();
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
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
public class TransactionalTestsCollection : ICollectionFixture<TestDatabaseFixture>
{
}
