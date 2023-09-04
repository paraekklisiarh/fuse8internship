using InternalApi.Configuration.Providers;
using Microsoft.EntityFrameworkCore;

namespace InternalApi.Configuration.Sources;

/// <summary>
/// This source is used for configuring the Entity Framework Core provider
/// </summary>
public class EFConfigurationSource : IConfigurationSource
{
    private readonly Action<DbContextOptionsBuilder> _optionsAction;

    /// <summary>
    /// Initializes a new instance of the <see cref="EFConfigurationSource"/> class
    /// </summary>
    /// <param name="optionsAction">The action to configure the <see cref="DbContextOptionsBuilder"/>.</param>
    public EFConfigurationSource(Action<DbContextOptionsBuilder> optionsAction)
    {
        _optionsAction = optionsAction;
    }

    /// <inheritdoc />
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new EFConfigurationProvider(_optionsAction);
    }
}