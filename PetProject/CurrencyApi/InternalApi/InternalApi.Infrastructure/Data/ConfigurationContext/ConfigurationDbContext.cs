using InternalApi.Entities;
using Microsoft.EntityFrameworkCore;

namespace InternalApi.Infrastructure.Data.ConfigurationContext;

public class ConfigurationDbContext : DbContext
{
    public ConfigurationDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<ConfigurationEntity> ConfigurationEntities { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("cur");
        
        base.OnModelCreating(modelBuilder);
    }
}