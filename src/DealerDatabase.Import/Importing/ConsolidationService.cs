namespace DealerDatabase.Import.Importing;

using System.Text.Json;
using DealerDatabase.Data.Entities;
using DealerDatabase.Import.Matching;
using DealerDatabase.Import.Abstractions;
using Microsoft.Extensions.Logging;

public sealed class ConsolidationService(ILogger<ConsolidationService> logger) : IConsolidationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public List<Dealer> BuildDealers(MatchOutput output)
    {
        logger.LogInformation("Consolidation started for {ClusterCount} matched clusters", output.Clusters.Count);

        var dealers = output.Clusters.Select(BuildDealer).ToList();

        var withDirectors = dealers.Count(d => d.Directors.Count > 0);
        var withSourceRecords = dealers.Count(d => d.SourceRecords.Count > 0);
        logger.LogInformation(
            "Consolidation complete. Dealers={DealerCount}, WithDirectors={WithDirectors}, WithSourceRecords={WithSourceRecords}",
            dealers.Count,
            withDirectors,
            withSourceRecords);

        return dealers;
    }

    private static Dealer BuildDealer(MatchCluster cluster)
    {
        var records = cluster.Records.Select(x => x.Record).ToList();
        var dealer = new Dealer
        {
            Name = BestTradingName(records) ?? BestLegalName(records) ?? "Unnamed dealer",
            LegalCompanyName = BestString(records, "legal", r => r.LegalName),
            CompanyNumber = CanonicalCompanyNumber(BestString(records, "company-number", r => r.CompanyNumber)),
            IncorporationDate = BestDate(records, "incorporation", r => r.IncorporationDate),
            CompanyStatus = BestString(records, "company-status", r => r.CompanyStatus),

            RegisteredAddressLine1 = BestString(records, "registered-address", r => r.RegisteredAddressLine1),
            RegisteredAddressLine2 = BestString(records, "registered-address", r => r.RegisteredAddressLine2),
            RegisteredAddressLine3 = BestString(records, "registered-address", r => r.RegisteredAddressLine3),
            RegisteredCity = BestString(records, "registered-address", r => r.RegisteredCity),
            RegisteredCounty = BestString(records, "registered-address", r => r.RegisteredCounty),
            RegisteredPostcode = BestString(records, "registered-address", r => r.RegisteredPostcode),

            TradingAddressLine1 = BestString(records, "trading-address", r => r.TradingAddressLine1),
            TradingAddressLine2 = BestString(records, "trading-address", r => r.TradingAddressLine2),
            TradingAddressLine3 = BestString(records, "trading-address", r => r.TradingAddressLine3),
            TradingCity = BestString(records, "trading-address", r => r.TradingCity),
            TradingCounty = BestString(records, "trading-address", r => r.TradingCounty),
            TradingPostcode = BestString(records, "trading-address", r => r.TradingPostcode),

            PrimaryPhone = BestPhone(records),
            PrimaryEmail = BestEmail(records),
            PrimaryWebsite = BestWebsite(records),

            FcaReferenceNumber = BestString(records, "fca", r => r.SourceType == "FCA" ? r.FcaReferenceNumber : null),
            FcaStatus = BestString(records, "fca", r => r.SourceType == "FCA" ? r.FcaStatus : null),
            IcoRegistrationNumber = BestString(records, "ico", r => r.SourceType == "ICO" ? r.IcoRegistrationNumber : null),
            IcoExpiryDate = BestDate(records, "ico", r => r.SourceType == "ICO" ? r.IcoExpiryDate : null),
            SafStatus = BestString(records, "saf", r => r.SourceType == "SAF" ? r.SafStatus : null),
            SafExpiryDate = BestDate(records, "saf", r => r.SourceType == "SAF" ? r.SafExpiryDate : null),
            VatNumber = CanonicalVatNumber(BestString(records, "vat", r => r.SourceType == "VAT" && !string.Equals(r.VatValidationStatus, "Valid", StringComparison.OrdinalIgnoreCase) ? null : r.VatNumber)),
            VatValidationStatus = BestVatValidationStatus(records),

            FinanceCalculator = BestString(records, "finance", r => r.FinanceCalculator),
            FinanceLendersJson = JsonSerializer.Serialize(records.SelectMany(r => r.FinanceLenders).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x), JsonOptions),
            RepresentativeApr = BestString(records, "finance", r => r.RepresentativeApr),

            InventoryCount = BestInt(records, "stock", r => r.InventoryCount),
            AvgListedPrice = BestDecimal(records, "stock", r => r.AvgListedPrice),
            AvgSoldPrice = BestDecimal(records, "stock", r => r.AvgSoldPrice),
            AvgDaysInStock = BestInt(records, "stock", r => r.AvgDaysInStock),
            SoldLast30Days = BestInt(records, "stock", r => r.SoldLast30Days),
            VehicleTypes = BestString(records, "stock", r => r.VehicleTypes),
        };

        foreach (var matched in cluster.Records)
        {
            var source = matched.Record;
            var sourceEntity = new DealerSourceRecord
            {
                Dealer = dealer,
                SourceType = source.SourceType,
                SourceKey = source.SourceKey,
                RawDataJson = source.RawDataJson,
                MatchConfidence = matched.MatchConfidence,
                MatchEvidenceJson = JsonSerializer.Serialize(matched.Evidence.SelectMany(e => e.Reasons).Distinct(), JsonOptions),
                ImportedAtUtc = DateTime.UtcNow
            };
            dealer.SourceRecords.Add(sourceEntity);

            foreach (var tradingName in source.TradingNames.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                dealer.TradingNames.Add(new DealerTradingName
                {
                    Dealer = dealer,
                    SourceRecord = sourceEntity,
                    Name = tradingName
                });
            }

            foreach (var director in source.Directors)
            {
                if (string.IsNullOrWhiteSpace(director.Name)) continue;
                dealer.Directors.Add(new DealerDirector
                {
                    Dealer = dealer,
                    SourceRecord = sourceEntity,
                    Name = director.Name,
                    Role = director.Role,
                    Occupation = director.Occupation,
                    Nationality = director.Nationality,
                    AppointedOn = director.AppointedOn,
                    ResignedOn = director.ResignedOn
                });
            }
        }

        // Keep provenance for every supplied scalar, not just the winning source.
        // This makes conflicts inspectable without losing the canonical value.
        foreach (var matched in cluster.Records)
        {
            var sourceEntity = dealer.SourceRecords.Single(x =>
                x.SourceType == matched.Record.SourceType && x.SourceKey == matched.Record.SourceKey);
            AddScalarSource(dealer, sourceEntity, "Name", matched.Record.TradingNames.FirstOrDefault() ?? matched.Record.LegalName);
            AddScalarSource(dealer, sourceEntity, "LegalCompanyName", matched.Record.LegalName);
            AddScalarSource(dealer, sourceEntity, "CompanyNumber", matched.Record.CompanyNumber is null ? null : CanonicalCompanyNumber(matched.Record.CompanyNumber));
            AddScalarSource(dealer, sourceEntity, "IncorporationDate", matched.Record.IncorporationDate?.ToString("yyyy-MM-dd"));
            AddScalarSource(dealer, sourceEntity, "CompanyStatus", matched.Record.CompanyStatus);
            AddScalarSource(dealer, sourceEntity, "RegisteredPostcode", matched.Record.RegisteredPostcode);
            AddScalarSource(dealer, sourceEntity, "TradingPostcode", matched.Record.TradingPostcode);
            var primaryPhone = matched.Record.Phones.FirstOrDefault();
            AddScalarSource(dealer, sourceEntity, "PrimaryPhone", primaryPhone is null ? null : FormatPhone(primaryPhone));
            AddScalarSource(dealer, sourceEntity, "PrimaryEmail", matched.Record.Emails.FirstOrDefault());
            AddScalarSource(dealer, sourceEntity, "PrimaryWebsite", matched.Record.Websites.FirstOrDefault());
            AddScalarSource(dealer, sourceEntity, "FcaReferenceNumber", matched.Record.FcaReferenceNumber);
            AddScalarSource(dealer, sourceEntity, "FcaStatus", matched.Record.FcaStatus);
            AddScalarSource(dealer, sourceEntity, "IcoRegistrationNumber", matched.Record.IcoRegistrationNumber);
            AddScalarSource(dealer, sourceEntity, "IcoExpiryDate", matched.Record.IcoExpiryDate?.ToString("yyyy-MM-dd"));
            AddScalarSource(dealer, sourceEntity, "SafStatus", matched.Record.SafStatus);
            AddScalarSource(dealer, sourceEntity, "SafExpiryDate", matched.Record.SafExpiryDate?.ToString("yyyy-MM-dd"));
            if (matched.Record.SourceType == "VAT" && !string.Equals(matched.Record.VatValidationStatus, "Valid", StringComparison.OrdinalIgnoreCase))
                AddScalarSource(dealer, sourceEntity, "VatLookupAttemptedNumber", matched.Record.VatNumber is null ? null : CanonicalVatNumber(matched.Record.VatNumber));
            else
                AddScalarSource(dealer, sourceEntity, "VatNumber", matched.Record.VatNumber is null ? null : CanonicalVatNumber(matched.Record.VatNumber));
            AddScalarSource(dealer, sourceEntity, "VatValidationStatus", matched.Record.VatValidationStatus);
            AddScalarSource(dealer, sourceEntity, "FinanceCalculator", matched.Record.FinanceCalculator);
            AddScalarSource(dealer, sourceEntity, "RepresentativeApr", matched.Record.RepresentativeApr);
            AddScalarSource(dealer, sourceEntity, "InventoryCount", matched.Record.InventoryCount?.ToString());
            AddScalarSource(dealer, sourceEntity, "AvgListedPrice", matched.Record.AvgListedPrice?.ToString("0.00"));
            AddScalarSource(dealer, sourceEntity, "AvgSoldPrice", matched.Record.AvgSoldPrice?.ToString("0.00"));
        }

        return dealer;
    }

    private static string? BestTradingName(IEnumerable<SourceDealerRecord> records)
        => records.OrderByDescending(SourcePriorityForTradingName)
            .SelectMany(r => r.TradingNames)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

    private static string? BestLegalName(IEnumerable<SourceDealerRecord> records)
        => records.OrderByDescending(r => SourcePriority(r, "legal"))
            .Select(r => r.LegalName)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

    private static string? BestString(IEnumerable<SourceDealerRecord> records, string field, Func<SourceDealerRecord, string?> selector)
        => records.OrderByDescending(r => SourcePriority(r, field))
            .Select(selector)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

    private static DateTime? BestDate(IEnumerable<SourceDealerRecord> records, string field, Func<SourceDealerRecord, DateTime?> selector)
        => records.OrderByDescending(r => SourcePriority(r, field)).Select(selector).FirstOrDefault(x => x.HasValue);

    private static int? BestInt(IEnumerable<SourceDealerRecord> records, string field, Func<SourceDealerRecord, int?> selector)
        => records.OrderByDescending(r => SourcePriority(r, field)).Select(selector).FirstOrDefault(x => x.HasValue);

    private static decimal? BestDecimal(IEnumerable<SourceDealerRecord> records, string field, Func<SourceDealerRecord, decimal?> selector)
        => records.OrderByDescending(r => SourcePriority(r, field)).Select(selector).FirstOrDefault(x => x.HasValue);

    private static bool? BestBool(IEnumerable<SourceDealerRecord> records, string field, Func<SourceDealerRecord, bool?> selector)
        => records.OrderByDescending(r => SourcePriority(r, field)).Select(selector).FirstOrDefault(x => x.HasValue);

    private static string? BestVatValidationStatus(IEnumerable<SourceDealerRecord> records)
    {
        if (records.Any(r => r.SourceType == "VAT" && string.Equals(r.VatValidationStatus, "Valid", StringComparison.OrdinalIgnoreCase)))
            return "Valid";
        return records.Any(r => r.SourceType == "VAT" && string.Equals(r.VatValidationStatus, "Not found", StringComparison.OrdinalIgnoreCase))
            ? "Not found"
            : null;
    }

    private static string? BestPhone(IEnumerable<SourceDealerRecord> records)
        => records.OrderByDescending(r => SourcePriority(r, "contact"))
            .SelectMany(r => r.Phones)
            .Select(FormatPhone)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

    private static string? BestEmail(IEnumerable<SourceDealerRecord> records)
        => records.OrderByDescending(r => SourcePriority(r, "contact"))
            .SelectMany(r => r.Emails)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

    private static string? BestWebsite(IEnumerable<SourceDealerRecord> records)
        => records.OrderByDescending(r => SourcePriority(r, "contact"))
            .SelectMany(r => r.Websites)
            .Select(Normalizers.CleanWebsite)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

    private static int SourcePriorityForTradingName(SourceDealerRecord r) => r.SourceType switch
    {
        "CRW" => 100,
        "MC" => 90,
        "FCA" => 80,
        "SAF" => 75,
        "ICO" => 70,
        "CH" => 60,
        "VAT" => 50,
        _ => 0
    };

    private static int SourcePriority(SourceDealerRecord r, string field) => field switch
    {
        "legal" or "company-number" or "incorporation" or "company-status" or "registered-address" => r.SourceType switch
        {
            "CH" => 100,
            "FCA" => 90,
            "ICO" => 80,
            "VAT" => 70,
            "SAF" => 60,
            _ => 30
        },
        "trading-address" => r.SourceType switch
        {
            "CRW" => 100,
            "MC" => 90,
            "SAF" => 80,
            "FCA" => 60,
            "ICO" => 50,
            _ => 20
        },
        "contact" => r.SourceType switch
        {
            "CRW" => 100,
            "MC" => 90,
            "SAF" => 80,
            _ => 40
        },
        "fca" => r.SourceType == "FCA" ? 100 : 0,
        "ico" => r.SourceType == "ICO" ? 100 : 0,
        "saf" => r.SourceType == "SAF" ? 100 : 0,
        "vat" => r.SourceType == "VAT" ? 100 : (r.SourceType == "CRW" ? 80 : 0),
        "finance" => r.SourceType == "CRW" ? 100 : 0,
        "stock" => r.SourceType == "MC" ? 100 : (r.SourceType == "CRW" ? 80 : 0),
        "web-metrics" => r.SourceType == "CRW" ? 100 : 0,
        _ => 0
    };

    private static string? CanonicalCompanyNumber(string? value)
    {
        var normalized = Normalizers.NormalizeCompanyNumber(value);
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string? CanonicalVatNumber(string? value)
    {
        var normalized = Normalizers.NormalizeVatNumber(value);
        return string.IsNullOrWhiteSpace(normalized) ? null : "GB" + normalized;
    }

    private static string FormatPhone(string value)
        => value.StartsWith("44", StringComparison.Ordinal) ? "+" + value : value;

    private static void AddScalarSource(Dealer dealer, DealerSourceRecord source, string fieldName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        if (dealer.FieldSources.Any(x => x.FieldName == fieldName && x.SourceRecord == source && x.Value == value)) return;
        dealer.FieldSources.Add(new DealerFieldSource
        {
            Dealer = dealer,
            SourceRecord = source,
            FieldName = fieldName,
            Value = value.Trim()
        });
    }
}

