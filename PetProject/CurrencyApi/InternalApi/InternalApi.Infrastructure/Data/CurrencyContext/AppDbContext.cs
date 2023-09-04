using InternalApi.Entities;
using Microsoft.EntityFrameworkCore;

namespace InternalApi.Infrastructure.Data.CurrencyContext;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    }

    public DbSet<Currency> Currencies { get; set; }
    
    public DbSet<CurrencyConversionTask> CurrencyConversionTasks { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("cur");

        builder.Entity<Currency>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => new { p.Code, p.RateDate }).IsUnique();
        });

        builder.Entity<CurrencyConversionTask>(e =>
        {
            e.HasKey(t => t.Id);
        });

        base.OnModelCreating(builder);
    }
}