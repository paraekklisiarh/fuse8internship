using Fuse8_ByteMinds.SummerSchool.PublicApi.Models;
using Microsoft.EntityFrameworkCore;

namespace Fuse8_ByteMinds.SummerSchool.PublicApi;

/// <summary>
///     Представляет контекст базы данных приложения
/// </summary>
public class AppDbContext : DbContext
{
    /// <inheritdoc />
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    /// <summary>
    ///     Gets or sets the DbSet for the FavouriteCurrency table
    /// </summary>
    public DbSet<FavouriteCurrency> FavouriteCurrencies { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the DbSet for the CurrencyApiSetting table
    /// </summary>
    public DbSet<CurrencyApiSetting> CurrencyApiSettings { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("user");
        builder.Entity<FavouriteCurrency>(e =>
        {
            e.HasIndex(favouriteCurrency => new { favouriteCurrency.BaseCurrency, favouriteCurrency.Currency })
                .IsUnique();
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