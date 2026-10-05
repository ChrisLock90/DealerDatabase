namespace DealerDatabase.Import.Importing;

using DealerDatabase.Data;
using DealerDatabase.Import.Abstractions;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Xml.Linq;

public sealed class SourceDataLoader(ILogger<SourceDataLoader> logger) : ISourceDataLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public IReadOnlyList<SourceDealerRecord> LoadAll()
    {
        logger.LogInformation("Loading source files from {DataDirectory}", SolutionPaths.DataDirectory);

        var records = new List<SourceDealerRecord>();

        records.AddRange(LoadSource("MC", "marketcheck_dealers.csv", LoadMarketCheck));
        records.AddRange(LoadSource("CRW", "crawled_dealers.csv", LoadCrawled));
        records.AddRange(LoadSource("CH", "companies_house.json", LoadCompaniesHouse));
        records.AddRange(LoadSource("FCA", "fca_register.json", LoadFca));
        records.AddRange(LoadSource("ICO", "ico_register.csv", LoadIco));
        records.AddRange(LoadSource("SAF", "saf_members.xml", LoadSaf));

        var vatPath = Path.Combine(SolutionPaths.DataDirectory, "vat_lookups");
        if (!Directory.Exists(vatPath))
        {
            logger.LogWarning("VAT lookup directory was not found at {Path}. Continuing without VAT source records.", vatPath);
        }
        records.AddRange(LoadSource("VAT", "vat_lookups", LoadVatDirectory));

        logger.LogInformation("Finished source loading. Total records={RecordCount}", records.Count);
        return records;
    }

    private List<SourceDealerRecord> LoadSource(string sourceType, string relativePath, Func<string, IEnumerable<SourceDealerRecord>> loader)
    {
        var fullPath = Path.Combine(SolutionPaths.DataDirectory, relativePath);
        logger.LogInformation("Loading source {SourceType} from {Path}", sourceType, fullPath);

        try
        {
            var loaded = loader(fullPath).ToList();
            logger.LogInformation("Loaded {RecordCount} records from source {SourceType}", loaded.Count, sourceType);
            return loaded;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed loading source {SourceType} from {Path}", sourceType, fullPath);
            throw;
        }
    }

    private static IEnumerable<SourceDealerRecord> LoadMarketCheck(string path)
    {
        foreach (var r in CsvReader.Read(path))
        {
            yield return new SourceDealerRecord
            {
                SourceType = "MC",
                SourceKey = Require(r, "mc_dealer_id"),
                RawDataJson = JsonSerializer.Serialize(r, JsonOptions),
                LegalName = null,
                TradingNames = Distinct(EmptyToNull(r["seller_name"])),
                TradingAddressLine1 = EmptyToNull(r["street"]),
                TradingCity = EmptyToNull(r["city"]),
                TradingCounty = EmptyToNull(r["county"]),
                TradingPostcode = EmptyToNull(r["postcode"]),
                Phones = NormalizedList(r["phone"], Normalizers.NormalizePhone),
                Emails = NormalizedList(r["email"], Normalizers.NormalizeEmail),
                Websites = NormalizedWebsites(r["website"]),
                InventoryCount = Normalizers.ParseInt(r["inventory_count"]),
                AvgListedPrice = Normalizers.ParseDecimal(r["avg_listed_price"]),
                AvgSoldPrice = Normalizers.ParseDecimal(r["avg_sold_price"]),
                AvgDaysInStock = Normalizers.ParseInt(r["avg_days_in_stock"]),
                SoldLast30Days = Normalizers.ParseInt(r["sold_last_30_days"]),
                VehicleTypes = EmptyToNull(r["vehicle_types"]),
                RepresentativeApr = null,
            };
        }
    }

    private static IEnumerable<SourceDealerRecord> LoadCrawled(string path)
    {
        foreach (var r in CsvReader.Read(path))
        {
            var footer = r["footer_text"] ?? string.Empty;
            var detectedCompanyNumber = FirstNonEmpty(r["company_number_detected"], ExtractCompanyNumber(footer));
            var detectedVat = FirstNonEmpty(Normalizers.NormalizeVatNumber(r["vat_number_detected"]), ExtractVat(footer));
            var detectedFca = FirstNonEmpty(Normalizers.NormalizeFcaNumber(r["fca_frn_detected"]), ExtractFca(footer));
            var financeLenders = Normalizers.SplitValues(r["finance_lenders_detected"]).ToList();

            yield return new SourceDealerRecord
            {
                SourceType = "CRW",
                SourceKey = Require(r, "crawl_id"),
                RawDataJson = JsonSerializer.Serialize(r, JsonOptions),
                LegalName = null,
                TradingNames = Distinct(FirstNonEmpty(r["business_name_detected"], r["h1"])),
                CompanyNumber = CleanDigitsOrText(detectedCompanyNumber),
                VatNumber = CleanDigitsOrText(detectedVat),
                FcaReferenceNumber = CleanDigitsOrText(detectedFca),
                TradingAddressLine1 = EmptyToNull(r["address_detected"]),
                TradingPostcode = EmptyToNull(FirstNonEmpty(r["postcode_detected"], TryExtractPostcode(r["address_detected"]))),
                Phones = NormalizedList(r["phones_detected"], Normalizers.NormalizePhone),
                Emails = NormalizedList(r["emails_detected"], Normalizers.NormalizeEmail),
                Websites = NormalizedWebsites(FirstNonEmpty(r["final_url"], r["source_url"])),
                FinanceCalculator = EmptyToNull(r["finance_calculator"]),
                FinanceLenders = financeLenders,
                RepresentativeApr = EmptyToNull(r["representative_apr"]),
                GoogleRating = FirstRating(r["google_rating"]),
                GoogleReviewCount = Normalizers.ParseInt(r["google_review_count"]),
                TrustpilotScore = Normalizers.ParseDecimal(r["trustpilot_score"]),
                StockCountDetected = Normalizers.ParseInt(r["stock_count_detected"]),
                PartExchange = Normalizers.ParseBool(r["part_exchange"]),
                WarrantyOffered = EmptyToNull(r["warranty_offered"]),
                OpeningHours = EmptyToNull(r["opening_hours_raw"]),
                FacebookUrl = EmptyToNull(r["facebook_url"]),
                InstagramUrl = EmptyToNull(r["instagram_url"]),
                WebsitePlatform = EmptyToNull(r["website_platform"]),
                CmsVersion = EmptyToNull(r["cms_version"])
            };
        }
    }

    private static IEnumerable<SourceDealerRecord> LoadCompaniesHouse(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        foreach (var item in document.RootElement.GetProperty("items").EnumerateArray())
        {
            var address = item.GetPropertyOrDefault("registered_office_address");
            var directors = new List<DirectorRecord>();
            if (item.TryGetProperty("officers", out var officers) && officers.ValueKind == JsonValueKind.Array)
            {
                foreach (var officer in officers.EnumerateArray())
                {
                    directors.Add(new DirectorRecord
                    {
                        Name = officer.GetStringOrNull("name") ?? string.Empty,
                        Role = officer.GetStringOrNull("officer_role"),
                        Occupation = officer.GetStringOrNull("occupation"),
                        Nationality = officer.GetStringOrNull("nationality"),
                        AppointedOn = Normalizers.ParseDate(officer.GetStringOrNull("appointed_on")),
                        ResignedOn = Normalizers.ParseDate(officer.GetStringOrNull("resigned_on"))
                    });
                }
            }

            var companyNumber = item.GetStringOrNull("company_number");
            yield return new SourceDealerRecord
            {
                SourceType = "CH",
                SourceKey = Require(companyNumber, "Companies House company_number"),
                RawDataJson = item.GetRawText(),
                LegalName = item.GetStringOrNull("company_name"),
                CompanyNumber = companyNumber,
                IncorporationDate = Normalizers.ParseDate(item.GetStringOrNull("date_of_creation")),
                CompanyStatus = item.GetStringOrNull("company_status"),
                RegisteredAddressLine1 = address.GetStringOrNull("address_line_1"),
                RegisteredAddressLine2 = address.GetStringOrNull("address_line_2"),
                RegisteredCity = address.GetStringOrNull("locality"),
                RegisteredCounty = address.GetStringOrNull("region"),
                RegisteredPostcode = address.GetStringOrNull("postal_code"),
                Directors = directors
            };
        }
    }

    private static IEnumerable<SourceDealerRecord> LoadFca(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        foreach (var item in document.RootElement.GetProperty("Data").EnumerateArray())
        {
            var address = item.GetPropertyOrDefault("Address");
            var tradingNames = new List<string>();
            if (item.TryGetProperty("Trading Names", out var names) && names.ValueKind == JsonValueKind.Array)
                tradingNames.AddRange(names.EnumerateArray().Select(x => x.GetString()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!));

            yield return new SourceDealerRecord
            {
                SourceType = "FCA",
                SourceKey = Require(item.GetStringOrNumber("FRN"), "FCA FRN"),
                RawDataJson = item.GetRawText(),
                LegalName = item.GetStringOrNull("Organisation Name"),
                TradingNames = tradingNames,
                CompanyNumber = item.GetStringOrNumber("Companies House Number"),
                FcaReferenceNumber = item.GetStringOrNumber("FRN"),
                FcaStatus = item.GetStringOrNull("Status"),
                RegisteredAddressLine1 = address.GetStringOrNull("Address Line 1"),
                RegisteredAddressLine2 = address.GetStringOrNull("Address Line 2"),
                RegisteredCity = address.GetStringOrNull("Town"),
                RegisteredPostcode = address.GetStringOrNull("Postcode")
            };
        }
    }

    private static IEnumerable<SourceDealerRecord> LoadIco(string path)
    {
        foreach (var r in CsvReader.Read(path))
        {
            var postcode = EmptyToNull(r["Organisation_postcode"]);
            yield return new SourceDealerRecord
            {
                SourceType = "ICO",
                SourceKey = Require(r, "Registration_number"),
                RawDataJson = JsonSerializer.Serialize(r, JsonOptions),
                LegalName = EmptyToNull(r["Organisation_name"]),
                TradingNames = Normalizers.SplitValues(r["Trading_names"]).ToList(),
                CompanyNumber = EmptyToNull(r["Company_registration_number"]),
                RegisteredAddressLine1 = EmptyToNull(r["Organisation_address_line_1"]),
                RegisteredAddressLine2 = EmptyToNull(r["Organisation_address_line_2"]),
                RegisteredAddressLine3 = EmptyToNull(r["Organisation_address_line_3"]),
                RegisteredPostcode = postcode,
                IcoRegistrationNumber = EmptyToNull(r["Registration_number"]),
                IcoExpiryDate = Normalizers.ParseDate(r["End_date_of_registration"])
            };
        }
    }

    private static IEnumerable<SourceDealerRecord> LoadSaf(string path)
    {
        var root = XDocument.Load(path).Root ?? throw new InvalidDataException("SAF XML has no root element.");
        foreach (var member in root.Elements("Member"))
        {
            var tradingName = member.Element("TradingAs")?.Value;
            yield return new SourceDealerRecord
            {
                SourceType = "SAF",
                SourceKey = Require(member.Attribute("id")?.Value, "SAF member id"),
                RawDataJson = JsonSerializer.Serialize(member.Elements().ToDictionary(x => x.Name.LocalName, x => x.Value), JsonOptions),
                LegalName = EmptyToNull(member.Element("Name")?.Value),
                TradingNames = Distinct(tradingName),
                TradingCity = EmptyToNull(member.Element("Town")?.Value),
                TradingPostcode = EmptyToNull(member.Element("Postcode")?.Value),
                Phones = NormalizedList(member.Element("Telephone")?.Value, Normalizers.NormalizePhone),
                Websites = NormalizedWebsites(member.Element("Website")?.Value),
                SafMemberId = member.Attribute("id")?.Value,
                SafStatus = EmptyToNull(member.Element("Status")?.Value),
                SafExpiryDate = Normalizers.ParseDate(member.Element("Expiry")?.Value)
            };
        }
    }

    private static IEnumerable<SourceDealerRecord> LoadVatDirectory(string path)
    {
        if (!Directory.Exists(path)) yield break;

        foreach (var file in Directory.EnumerateFiles(path, "*.json", SearchOption.TopDirectoryOnly).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(file));
            var root = document.RootElement;
            var target = root.GetPropertyOrDefault("target");
            var valid = target.HasValue && target.Value.ValueKind == JsonValueKind.Object;
            var queriedVatNumber = Path.GetFileNameWithoutExtension(file);
            if (queriedVatNumber.StartsWith("GB", StringComparison.OrdinalIgnoreCase))
                queriedVatNumber = queriedVatNumber[2..];

            var vatNumber = valid
                ? target.GetStringOrNull("vatNumber") ?? queriedVatNumber
                : queriedVatNumber;
            var address = valid ? target?.GetPropertyOrDefault("address") : null;

            yield return new SourceDealerRecord
            {
                SourceType = "VAT",
                SourceKey = Require(vatNumber, "VAT number"),
                RawDataJson = root.GetRawText(),
                LegalName = valid ? target.GetStringOrNull("name") : null,
                VatNumber = vatNumber,
                VatValidationStatus = valid ? "Valid" : "Not found",
                RegisteredAddressLine1 = address.GetStringOrNull("line1"),
                RegisteredAddressLine2 = address.GetStringOrNull("line2"),
                RegisteredAddressLine3 = address.GetStringOrNull("line3"),
                RegisteredPostcode = address.GetStringOrNull("postcode")
            };
        }
    }

    private static string Require(Dictionary<string, string> row, string key)
        => Require(row.TryGetValue(key, out var value) ? value : null, key);

    private static string Require(string? value, string label)
        => string.IsNullOrWhiteSpace(value) ? throw new InvalidDataException($"Required {label} was missing.") : value.Trim();

    private static string? EmptyToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim();

    private static string CleanDigitsOrText(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    private static List<string> Distinct(string? value)
        => string.IsNullOrWhiteSpace(value) ? [] : [value.Trim()];

    private static List<string> NormalizedList(string? value, Func<string?, string> normalizer)
        => Normalizers.SplitValues(value).Select(normalizer).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    private static List<string> NormalizedWebsites(string? value)
        => Normalizers.SplitValues(value).Select(Normalizers.CleanWebsite).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    private static decimal? FirstRating(string? value)
    {
        var clean = value?.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return Normalizers.ParseDecimal(clean);
    }

    private static string? ExtractCompanyNumber(string text)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            text,
            @"(?:Registered in (?:England|Scotland)\s*(?:& Wales)?\s*No\.?|Co\.\s*No\.?)\s*([A-Z]{0,2}\s*\d{6,8})",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Replace(" ", string.Empty, StringComparison.Ordinal) : null;
    }

    private static string? ExtractVat(string text)
    {
        var match = System.Text.RegularExpressions.Regex.Match(text, @"(?:VAT(?:\s+No\.?|\s+Reg\.?|\s+Number)?:?\s*)(?:GB\s*)?(\d{9})", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string? ExtractFca(string text)
    {
        var match = System.Text.RegularExpressions.Regex.Match(text, @"(?:FCA|Firm Reference Number|FRN)[^0-9]{0,20}(\d{5,7})", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string? TryExtractPostcode(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var match = System.Text.RegularExpressions.Regex.Match(text, @"\b([A-Z]{1,2}\d{1,2}[A-Z]?\s*\d[A-Z]{2})\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }
}

internal static class JsonElementExtensions
{
    public static JsonElement? GetPropertyOrDefault(this JsonElement element, string name)
        => element.TryGetProperty(name, out var value) ? value : null;

    public static string? GetStringOrNull(this JsonElement? element, string name)
        => element.HasValue ? element.Value.GetStringOrNull(name) : null;

    public static string? GetStringOrNull(this JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value)) return null;
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.ToString(),
            _ => null
        };
    }

    public static string? GetStringOrNumber(this JsonElement element, string name)
        => element.GetStringOrNull(name);
}
