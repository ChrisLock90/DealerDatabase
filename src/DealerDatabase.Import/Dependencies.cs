using Microsoft.Extensions.DependencyInjection;
using DealerDatabase.Import.Importing;
using DealerDatabase.Import.Matching;
using DealerDatabase.Import.Abstractions;

namespace DealerDatabase.Import;

public static class Dependencies
{
    public static IServiceCollection AddDealerImportServices(this IServiceCollection services)
    {
        // Import pipeline services
        services.AddTransient<ISourceDataLoader, SourceDataLoader>();
        services.AddTransient<IDealerMatcher, DealerMatcher>();
        services.AddTransient<IConsolidationService, ConsolidationService>();

        return services;
    }
}
