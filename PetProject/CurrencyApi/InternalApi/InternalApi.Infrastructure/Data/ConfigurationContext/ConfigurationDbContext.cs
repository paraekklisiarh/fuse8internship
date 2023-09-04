using InternalApi.Entities;
using Microsoft.EntityFrameworkCore;

namespace InternalApi.Infrastructure.Data.ConfigurationContext;

public class ConfigurationDbContext : DbContext
{
    public ConfigurationDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<ConfigurationEntity> ConfigurationEntities { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("cur");
        
        base.OnModelCreating(modelBuilder);
    }
}