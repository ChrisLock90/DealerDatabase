using DealerDatabase.Data.Entities;

namespace DealerDatabase.Import.Importing;

public static class ConsolidationServiceExtensions
{
    // Public helper extension used by the unit test and the import pipeline.
    public static IList<Dealer> DeduplicateDirectors(this Abstractions.IConsolidationService _svc, IList<Dealer> dealers)
    {
        foreach (var dealer in dealers)
        {
            if (dealer.Directors == null || dealer.Directors.Count <= 1)
                continue;

            // Normalize name and role for grouping using a string key to avoid tuple comparer issues
            var groups = dealer.Directors
                .Where(d => !string.IsNullOrWhiteSpace(d.Name))
                .GroupBy(d => (d.Name.Trim().ToUpperInvariant() + "|" + (d.Role ?? string.Empty).Trim().ToUpperInvariant()))
                .ToList();

            var merged = new List<DealerDirector>();

            foreach (var g in groups)
            {
                var items = g.ToList();
                var first = items[0];

                // Merge strategy: prefer non-empty values, else first
                foreach (var alt in items.Skip(1))
                {
                    if (string.IsNullOrWhiteSpace(first.Occupation) && !string.IsNullOrWhiteSpace(alt.Occupation))
                        first.Occupation = alt.Occupation;
                    if (string.IsNullOrWhiteSpace(first.Nationality) && !string.IsNullOrWhiteSpace(alt.Nationality))
                        first.Nationality = alt.Nationality;
                    if (!first.AppointedOn.HasValue && alt.AppointedOn.HasValue)
                        first.AppointedOn = alt.AppointedOn;
                    if (!first.ResignedOn.HasValue && alt.ResignedOn.HasValue)
                        first.ResignedOn = alt.ResignedOn;                    
                }

                // Ensure normalized name/role are stored (trimmed from the first item)
                first.Name = items[0].Name.Trim();
                first.Role = (items[0].Role ?? string.Empty).Trim();

                merged.Add(first);

                // Logging hook: if duplicates were merged, notify (the import app will log)
                if (items.Count > 1)
                {
                    first.MergeNote = (first.MergeNote ?? string.Empty) + $"Merged {items.Count} director entries;";
                }
            }

            dealer.Directors = merged;
        }

        return dealers;
    }
}
