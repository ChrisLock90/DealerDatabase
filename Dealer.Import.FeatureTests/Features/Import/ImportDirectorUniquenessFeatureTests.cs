namespace Dealer.Import.FeatureTests.Features.Import;

using Dealer.Import.FeatureTests.Infrastructure;
using DealerDatabase.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

[TestFixture]
public class ImportDirectorUniquenessFeatureTests
{
    [Test]
    public async Task Imported_directors_are_unique_per_dealer_name_and_role()
    {
        await using var runner = new FeatureImportRunner();
        await runner.RunOnceAsync();

        await using var scope = runner.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DealerDbContext>();

        var duplicates = await db.DealerDirectors
            .AsNoTracking()
            .GroupBy(d => new { d.DealerId, Name = d.Name.Trim().ToUpper(), Role = (d.Role ?? string.Empty).Trim().ToUpper() })
            .Where(g => g.Count() > 1)
            .Select(g => new { g.Key.DealerId, g.Key.Name, g.Key.Role, Count = g.Count() })
            .ToListAsync();

        Assert.That(duplicates, Is.Empty,
            "Expected no duplicate director entries per dealer by normalized Name+Role after deduplication.");
    }
}
