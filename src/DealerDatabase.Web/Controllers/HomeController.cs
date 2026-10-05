namespace DealerDatabase.Web.Controllers;

using DealerDatabase.Data;
using DealerDatabase.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

public class HomeController(DealerDbContext db, ILogger<HomeController> logger) : Controller
{
    public async Task<IActionResult> Index(string? q, CancellationToken cancellationToken)
    {
        q = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        logger.LogInformation("Dealer search requested. QueryProvided={QueryProvided}, QueryLength={QueryLength}", q is not null, q?.Length ?? 0);

        var query = db.Dealers.AsNoTracking().AsQueryable();

        if (q is not null)
        {
            var search = q.ToLower();
            query = query.Where(d =>
                d.Name.ToLower().Contains(search) ||
                (d.LegalCompanyName != null && d.LegalCompanyName.ToLower().Contains(search)) ||
                (d.CompanyNumber != null && d.CompanyNumber.Contains(q)) ||
                (d.FcaReferenceNumber != null && d.FcaReferenceNumber.Contains(q)) ||
                (d.VatNumber != null && d.VatNumber.Contains(q)));
        }

        var dealers = await query
            .OrderBy(d => d.Name)
            .Select(d => new DealerSummaryViewModel(
                d.Id, d.Name, d.LegalCompanyName, d.CompanyNumber,
                d.FcaReferenceNumber, d.FcaStatus,
                d.RegisteredPostcode, d.TradingPostcode,
                d.SourceRecords.Count))
            .ToListAsync(cancellationToken);

        logger.LogInformation("Dealer search completed. Results={ResultCount}", dealers.Count);
        return View(new DealerListViewModel(dealers, q));
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        logger.LogInformation("Dealer details requested for DealerId={DealerId}", id);

        var dealer = await db.Dealers
            .AsNoTracking()
            .Include(d => d.SourceRecords)
            .Include(d => d.TradingNames)
            .Include(d => d.Directors)
            .Include(d => d.FieldSources)
            .ThenInclude(f => f.SourceRecord)
            .SingleOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (dealer is null)
        {
            logger.LogWarning("Dealer details not found for DealerId={DealerId}", id);
            return NotFound();
        }

        var provenance = dealer.FieldSources
            .GroupBy(x => x.FieldName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<FieldProvenanceValue>)g
                    .GroupBy(x => x.Value?.Trim() ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                    .Select(valueGroup => new FieldProvenanceValue(
                        valueGroup.Key,
                        valueGroup
                            .Select(x => $"{x.SourceRecord.SourceType}:{x.SourceRecord.SourceKey}")
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                            .ToList()))
                    .OrderBy(x => x.Value, StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                StringComparer.OrdinalIgnoreCase);

        var labels = dealer.SourceRecords
            .OrderBy(x => x.SourceType)
            .ThenBy(x => x.SourceKey)
            .Select(x => $"{x.SourceType}:{x.SourceKey}")
            .ToList();

        logger.LogInformation(
            "Dealer details loaded for DealerId={DealerId}. Sources={SourceCount}, FieldProvenanceGroups={FieldGroupCount}, Directors={DirectorCount}",
            id,
            dealer.SourceRecords.Count,
            provenance.Count,
            dealer.Directors.Count);

        return View(new DealerDetailsViewModel(dealer, provenance, labels));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        logger.LogError("Error action invoked. TraceIdentifier={TraceIdentifier}", HttpContext.TraceIdentifier);
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
