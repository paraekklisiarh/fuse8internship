using InternalApi.Infrastructure.Data.CurrencyContext; 
using Microsoft.EntityFrameworkCore; 
using Testcontainers.PostgreSql; 
 
namespace InternalApi.Tests.Fixtures; 
 
/// <summary> 
/// База данных в контейтере 
/// </summary> 
public class DatabaseFixture : IAsyncLifetime 
{ 
    private readonly PostgreSqlContainer _container = 
        new PostgreSqlBuilder() 
            .WithCleanUp(true) 
            .Build(); 
 
    private string ConnectionString => _container.GetConnectionString(); 
 
    public async Task InitializeAsync() 
    { 
        await _container.StartAsync(); 
 
        var context = CreateAppContext(); 
         
        await context.Database.MigrateAsync(); 
        await context.Database.EnsureCreatedAsync(); 
    } 
 
    public AppDbContext CreateAppContext() 
    { 
        var context = new AppDbContext( 
            new DbContextOptionsBuilder<AppDbContext>() 
                .UseNpgsql(ConnectionString) 
                .EnableSensitiveDataLogging() 
                .EnableDetailedErrors() 
                .EnableThreadSafetyChecks() 
                .UseSnakeCaseNamingConvention() 
                .UseAllCheckConstraints() 
                .Options); 
         
        return context; 
    } 
 
    public void Cleanup() 
    { 
        using var appContext = CreateAppContext(); 
        appContext.Currencies.RemoveRange(appContext.Currencies); 
        appContext.SaveChanges(); 
    } 
 
    public async Task DisposeAsync() 
    { 
        await _container.DisposeAsync(); 
    } 
}