using InternalApi.Entities;
using Microsoft.EntityFrameworkCore;

namespace InternalApi.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    }

    public DbSet<Currency> Currencies { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("cur");

        builder.Entity<Currency>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => new { p.Code, p.RateDate }).IsUnique();
        });

        base.OnModelCreating(builder);
    }
}