namespace DealerDatabase.Import;

using DealerDatabase.Import.Abstractions;
using DealerDatabase.Import.Importing;
using DealerDatabase.Import.Matching;
using Microsoft.Extensions.DependencyInjection;

public static class Dependencies
{
    public static IServiceCollection AddDealerImportServices(this IServiceCollection services)
    {        
        services.AddTransient<ISourceDataLoader, SourceDataLoader>();
        services.AddTransient<IDealerMatcher, DealerMatcher>();
        services.AddTransient<IConsolidationService, ConsolidationService>();

        return services;
    }
}
