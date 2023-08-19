using Fuse8_ByteMinds.SummerSchool.PublicApi.Models;
using Microsoft.EntityFrameworkCore;

namespace Fuse8_ByteMinds.SummerSchool.PublicApi;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<FavouriteCurrency> FavouriteCurrencies { get; set; }
    
    public DbSet<CurrencyApiSetting> CurrencyApiSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("user");

        builder.Entity<FavouriteCurrency>(e =>
        {
            e.HasIndex(favouriteCurrency => new { favouriteCurrency.BaseCurrency, favouriteCurrency.Currency }).IsUnique();
            e.HasIndex(fc => fc.Name).IsUnique();
        });

        builder.Entity<CurrencyApiSetting>(typeBuilder =>
        {
            typeBuilder.HasKey(e => e.Id);
            typeBuilder.HasIndex(e => new { e.DefaultCurrency, e.CurrencyRoundCount });
        });

        base.OnModelCreating(builder);
    }
}