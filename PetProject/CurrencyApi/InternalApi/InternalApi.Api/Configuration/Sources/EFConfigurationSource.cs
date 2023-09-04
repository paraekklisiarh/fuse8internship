using InternalApi.Configuration.Providers;
using Microsoft.EntityFrameworkCore;

namespace InternalApi.Configuration.Sources;

public class EFConfigurationSource : IConfigurationSource
{
    private readonly Action<DbContextOptionsBuilder> _optionsAction;

    public EFConfigurationSource(Action<DbContextOptionsBuilder> optionsAction)
    {
        _optionsAction = optionsAction;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new EFConfigurationProvider(_optionsAction);
    }
}