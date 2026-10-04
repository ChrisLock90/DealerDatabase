using System.Diagnostics;
using DealerDatabase.Data;
using DealerDatabase.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DealerDatabase.Web.Controllers;

public class HomeController(DealerDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q, CancellationToken cancellationToken)
    {
        q = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
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

        return View(new DealerListViewModel(dealers, q));
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var dealer = await db.Dealers
            .AsNoTracking()
            .Include(d => d.SourceRecords)
            .Include(d => d.TradingNames)
            .Include(d => d.Directors)
            .Include(d => d.FieldSources)
            .ThenInclude(f => f.SourceRecord)
            .SingleOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (dealer is null) return NotFound();

        var provenance = dealer.FieldSources
            .GroupBy(x => x.FieldName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<string>)g
                    .Select(x => $"{x.Value} ({x.SourceRecord.SourceType}:{x.SourceRecord.SourceKey})")
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                StringComparer.OrdinalIgnoreCase);

        var labels = dealer.SourceRecords
            .OrderBy(x => x.SourceType)
            .ThenBy(x => x.SourceKey)
            .Select(x => $"{x.SourceType}:{x.SourceKey}")
            .ToList();

        return View(new DealerDetailsViewModel(dealer, provenance, labels));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
