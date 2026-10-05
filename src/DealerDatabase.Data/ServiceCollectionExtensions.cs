namespace DealerDatabase.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="DealerDbContext"/> using the shared SQLite database file.
    /// </summary>
    public static IServiceCollection AddDealerDatabase(this IServiceCollection services)
    {
        services.AddDbContext<DealerDbContext>(options =>
            options.UseSqlite($"Data Source={SolutionPaths.DatabaseFile}"));

        return services;
    }
}
