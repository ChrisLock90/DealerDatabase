namespace DealerDatabase.Import.Matching;

using DealerDatabase.Import.Importing;
using DealerDatabase.Import.Abstractions;

public sealed class DealerMatcher : IDealerMatcher
{
    private static readonly HashSet<string> DirectoryDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "ukcardealerdirectory.example"
    };

    public MatchOutput Match(IReadOnlyList<SourceDealerRecord> records)
    {
        var unionFind = new UnionFind(records.Count);
        var evidenceByRecord = new Dictionary<int, List<MatchEvidence>>();

        for (var i = 0; i < records.Count; i++)
            evidenceByRecord[i] = [];

        // Authoritative identifiers are allowed to join records without fuzzy corroboration.
        UnionOnKey(records, unionFind, evidenceByRecord,
            r => Normalizers.NormalizeCompanyNumber(r.CompanyNumber), "company-number", 1.00d);
        UnionOnKey(records, unionFind, evidenceByRecord,
            VatKey, "VAT-number", 0.995d);

        for (var i = 0; i < records.Count; i++)
        {
            for (var j = i + 1; j < records.Count; j++)
            {
                if (!MayContainMatch(records[i], records[j]))
                    continue;

                var result = Score(records[i], records[j]);
                if (!result.IsMatch)
                    continue;

                unionFind.Union(i, j);
                evidenceByRecord[i].Add(result.Evidence);
                evidenceByRecord[j].Add(result.Evidence with { LeftKey = result.Evidence.RightKey, RightKey = result.Evidence.LeftKey });
            }
        }

        var clusters = records
            .Select((record, index) => (record, index, root: unionFind.Find(index)))
            .GroupBy(x => x.root)
            .Select(g => new MatchCluster(
                g.Select(x => new MatchedRecord(x.index, x.record, BestConfidence(evidenceByRecord[x.index]), evidenceByRecord[x.index]))
                    .OrderBy(x => x.Record.SourceType, StringComparer.Ordinal)
                    .ThenBy(x => x.Record.SourceKey, StringComparer.OrdinalIgnoreCase)
                    .ToList()))
            .OrderBy(c => c.Records.Min(x => x.Record.SourceType), StringComparer.Ordinal)
            .ThenBy(c => c.Records.Min(x => x.Record.SourceKey), StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new MatchOutput(clusters);
    }

    private static void UnionOnKey(
        IReadOnlyList<SourceDealerRecord> records,
        UnionFind unionFind,
        Dictionary<int, List<MatchEvidence>> evidenceByRecord,
        Func<SourceDealerRecord, string> keySelector,
        string evidenceType,
        double confidence)
    {
        var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < records.Count; i++)
        {
            var key = keySelector(records[i]);
            if (string.IsNullOrWhiteSpace(key)) continue;
            if (!seen.TryGetValue(key, out var first))
            {
                seen[key] = i;
                continue;
            }

            if (records[first].SourceType.Equals(records[i].SourceType, StringComparison.OrdinalIgnoreCase) &&
                evidenceType is "company-number" or "VAT-number")
            {
                // Same-source duplicates with the same authoritative identifier are safe to merge.
            }

            var evidence = new MatchEvidence(
                records[first].SourceType, records[first].SourceKey,
                records[i].SourceType, records[i].SourceKey,
                confidence, [evidenceType]);
            unionFind.Union(first, i);
            evidenceByRecord[first].Add(evidence);
            evidenceByRecord[i].Add(evidence with { LeftKey = evidence.RightKey, RightKey = evidence.LeftKey });
        }
    }

    private static bool MayContainMatch(SourceDealerRecord left, SourceDealerRecord right)
    {
        if (left.SourceType.Equals(right.SourceType, StringComparison.OrdinalIgnoreCase) &&
            left.SourceType is "CH" or "FCA" or "VAT" or "SAF")
            return false;

        var leftCompany = Normalizers.NormalizeCompanyNumber(left.CompanyNumber);
        var rightCompany = Normalizers.NormalizeCompanyNumber(right.CompanyNumber);
        if (!string.IsNullOrWhiteSpace(leftCompany) && !string.IsNullOrWhiteSpace(rightCompany) && leftCompany != rightCompany)
            return false;

        var leftVat = VatKey(left);
        var rightVat = VatKey(right);
        if (!string.IsNullOrWhiteSpace(leftVat) && !string.IsNullOrWhiteSpace(rightVat) && leftVat != rightVat)
            return false;

        if (left.SourceType == "CRW" && right.SourceType == "CRW")
        {
            var domains = SharedDomains(left, right);
            if (domains.Count > 0 && domains.All(d => !DirectoryDomains.Contains(d)))
                return true;
        }

        return true;
    }

    private static PairScore Score(SourceDealerRecord left, SourceDealerRecord right)
    {
        var signals = new List<(string Name, double Strength, string Evidence)>();

        var company = Exact(Normalizers.NormalizeCompanyNumber(left.CompanyNumber), Normalizers.NormalizeCompanyNumber(right.CompanyNumber));
        if (company) signals.Add(("company number", 0.995d, "exact company registration number"));

        var vat = Exact(Normalizers.NormalizeVatNumber(left.VatNumber), Normalizers.NormalizeVatNumber(right.VatNumber)) &&
                  (IsValidatedVat(left) || IsValidatedVat(right));
        if (vat) signals.Add(("VAT number", 0.98d, "exact validated VAT number"));

        var frn = Exact(Normalizers.NormalizeFcaNumber(left.FcaReferenceNumber), Normalizers.NormalizeFcaNumber(right.FcaReferenceNumber));
        var nameSimilarity = BestNameSimilarity(left, right);
        var postcodeMatch = Exact(Normalizers.NormalizePostcode(left.TradingPostcode ?? left.RegisteredPostcode),
            Normalizers.NormalizePostcode(right.TradingPostcode ?? right.RegisteredPostcode));
        if (frn && (company || postcodeMatch || nameSimilarity >= 0.70d))
            signals.Add(("FCA FRN", 0.95d, "FCA FRN corroborated by company, postcode or name"));

        var sharedDomains = SharedDomains(left, right);
        var sharedPhones = Shared(NormalizedPhones(left), NormalizedPhones(right));
        var sharedEmails = Shared(NormalizedEmails(left), NormalizedEmails(right));

        if (sharedDomains.Count > 0 && !sharedDomains.Any(DirectoryDomains.Contains))
        {
            var domainStrength = postcodeMatch || sharedPhones.Count > 0 || sharedEmails.Count > 0 || nameSimilarity >= 0.85d
                ? 0.86d
                : 0d;
            if (domainStrength > 0)
                signals.Add(("website domain", domainStrength, $"shared website domain: {string.Join(", ", sharedDomains)}"));
        }

        if (sharedPhones.Count > 0)
        {
            var phoneStrength = nameSimilarity >= 0.60d || postcodeMatch || sharedDomains.Count > 0 ? 0.84d : 0d;
            if (phoneStrength > 0)
                signals.Add(("phone", phoneStrength, "shared normalised phone number"));
        }

        if (sharedEmails.Count > 0)
        {
            var emailStrength = nameSimilarity >= 0.60d || sharedDomains.Count > 0 ? 0.81d : 0d;
            if (emailStrength > 0)
                signals.Add(("email", emailStrength, "shared email address"));
        }

        if (postcodeMatch)
            signals.Add(("postcode", 0.60d, "exact postcode"));

        if (nameSimilarity >= 0.50d)
            signals.Add(("name", 0.70d * nameSimilarity, $"name similarity {nameSimilarity:P0}"));

        var addressSimilarity = AddressSimilarity(left, right);
        if (addressSimilarity >= 0.55d)
            signals.Add(("address", 0.55d * addressSimilarity, $"address similarity {addressSimilarity:P0}"));

        var directorSimilarity = DirectorSimilarity(left, right);
        if (directorSimilarity >= 0.60d)
            signals.Add(("director", 0.45d * directorSimilarity, $"director-name overlap {directorSimilarity:P0}"));

        var confidence = 1d;
        foreach (var signal in signals)
            confidence *= 1d - Math.Clamp(signal.Strength, 0d, 0.999d);
        confidence = 1d - confidence;

        // Domain/phone/email evidence is intentionally not enough on its own, since the
        // dataset contains shared principal-FCA details and repeated web directory records.
        var hasStrongIdentity = company || vat || (frn && (nameSimilarity >= 0.70d || postcodeMatch));
        var hasCorroboratedContact =
            (sharedDomains.Count > 0 && (postcodeMatch || nameSimilarity >= 0.55d || sharedPhones.Count > 0)) ||
            (sharedPhones.Count > 0 && (postcodeMatch || nameSimilarity >= 0.60d)) ||
            (sharedEmails.Count > 0 && (sharedDomains.Count > 0 || nameSimilarity >= 0.70d));
        var hasFuzzyIdentity = postcodeMatch && nameSimilarity >= 0.78d;
        // Address similarity is useful supporting evidence, but never sufficient by itself.
        // The supplied data intentionally reuses generic addresses such as "The Old Foundry".
        var isMatch = (hasStrongIdentity || hasCorroboratedContact || hasFuzzyIdentity);

        var evidenceText = signals.Count == 0
            ? []
            : signals.OrderByDescending(x => x.Strength).Select(x => x.Evidence).ToArray();

        return new PairScore(
            isMatch,
            confidence,
            new MatchEvidence(left.SourceType, left.SourceKey, right.SourceType, right.SourceKey, confidence, evidenceText));
    }

    private static double BestNameSimilarity(SourceDealerRecord left, SourceDealerRecord right)
    {
        var leftNames = Names(left).ToArray();
        var rightNames = Names(right).ToArray();
        if (leftNames.Length == 0 || rightNames.Length == 0) return 0d;
        return leftNames.SelectMany(l => rightNames.Select(r => Similarity(l, r))).DefaultIfEmpty(0d).Max();
    }

    private static double AddressSimilarity(SourceDealerRecord left, SourceDealerRecord right)
    {
        var candidatesLeft = new[] { RegisteredAddress(left), TradingAddress(left) }.Where(x => !string.IsNullOrWhiteSpace(x));
        var candidatesRight = new[] { RegisteredAddress(right), TradingAddress(right) }.Where(x => !string.IsNullOrWhiteSpace(x));
        return candidatesLeft.SelectMany(l => candidatesRight.Select(r => Similarity(l!, r!))).DefaultIfEmpty(0d).Max();
    }

    private static double DirectorSimilarity(SourceDealerRecord left, SourceDealerRecord right)
    {
        var a = left.Directors.Select(x => Normalizers.NormalizeName(x.Name)).Where(x => x.Length > 2).ToHashSet();
        var b = right.Directors.Select(x => Normalizers.NormalizeName(x.Name)).Where(x => x.Length > 2).ToHashSet();
        if (a.Count == 0 || b.Count == 0) return 0d;
        var overlap = a.Intersect(b).Count();
        return (double)overlap / Math.Min(a.Count, b.Count);
    }

    private static IEnumerable<string> Names(SourceDealerRecord record)
        => new[] { record.LegalName }.Concat(record.TradingNames).Where(x => !string.IsNullOrWhiteSpace(x)).Select(Normalizers.NormalizeName).Where(x => x.Length > 1);

    private static string RegisteredAddress(SourceDealerRecord r)
        => Normalizers.NormalizeLoose(string.Join(' ', new[] { r.RegisteredAddressLine1, r.RegisteredAddressLine2, r.RegisteredAddressLine3, r.RegisteredCity, r.RegisteredCounty, r.RegisteredPostcode }));

    private static string TradingAddress(SourceDealerRecord r)
        => Normalizers.NormalizeLoose(string.Join(' ', new[] { r.TradingAddressLine1, r.TradingAddressLine2, r.TradingAddressLine3, r.TradingCity, r.TradingCounty, r.TradingPostcode }));

    private static IEnumerable<string> NormalizedPhones(SourceDealerRecord r) => r.Phones.Select(Normalizers.NormalizePhone).Where(x => x.Length >= 9);
    private static IEnumerable<string> NormalizedEmails(SourceDealerRecord r) => r.Emails.Select(Normalizers.NormalizeEmail).Where(x => x.Length > 3);
    private static HashSet<string> SharedDomains(SourceDealerRecord a, SourceDealerRecord b)
        => Shared(a.Websites.Select(Normalizers.NormalizeDomain), b.Websites.Select(Normalizers.NormalizeDomain));

    private static HashSet<string> Shared(IEnumerable<string> a, IEnumerable<string> b)
        => a.Where(x => !string.IsNullOrWhiteSpace(x)).ToHashSet(StringComparer.OrdinalIgnoreCase)
            .Intersect(b.Where(x => !string.IsNullOrWhiteSpace(x)).ToHashSet(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static string VatKey(SourceDealerRecord record)
        => record.SourceType == "VAT" && IsValidatedVat(record)
            ? Normalizers.NormalizeVatNumber(record.VatNumber)
            : string.Empty;

    private static bool IsValidatedVat(SourceDealerRecord record)
        => record.SourceType == "VAT"
            ? string.Equals(record.VatValidationStatus, "Valid", StringComparison.OrdinalIgnoreCase)
            : false;

    private static bool Exact(string a, string b)
        => !string.IsNullOrWhiteSpace(a) && !string.IsNullOrWhiteSpace(b) && a.Equals(b, StringComparison.OrdinalIgnoreCase);

    private static double BestConfidence(IEnumerable<MatchEvidence> evidence)
        => evidence.Select(x => x.Confidence).DefaultIfEmpty(1d).Max();

    private static double Similarity(string left, string right)
    {
        if (left == right) return 1d;
        if (left.Length == 0 || right.Length == 0) return 0d;

        var distance = Levenshtein(left, right);
        var ratio = 1d - (double)distance / Math.Max(left.Length, right.Length);
        var tokenA = TokenSet(left);
        var tokenB = TokenSet(right);
        var jaccard = tokenA.Count == 0 && tokenB.Count == 0
            ? 1d
            : (double)tokenA.Intersect(tokenB).Count() / tokenA.Union(tokenB).Count();
        return Math.Max(ratio, jaccard);
    }

    private static HashSet<string> TokenSet(string text)
    {
        if (text.Length < 2) return [text];
        return Enumerable.Range(0, text.Length - 1)
            .Select(i => text.Substring(i, 2))
            .ToHashSet(StringComparer.Ordinal);
    }

    private static int Levenshtein(string a, string b)
    {
        var previous = new int[b.Length + 1];
        var current = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++) previous[j] = j;

        for (var i = 1; i <= a.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
            }
            (previous, current) = (current, previous);
        }
        return previous[b.Length];
    }

    private sealed class UnionFind(int count)
    {
        private readonly int[] parent = Enumerable.Range(0, count).ToArray();
        private readonly byte[] rank = new byte[count];

        public int Find(int value)
        {
            while (parent[value] != value)
            {
                parent[value] = parent[parent[value]];
                value = parent[value];
            }
            return value;
        }

        public void Union(int a, int b)
        {
            var rootA = Find(a);
            var rootB = Find(b);
            if (rootA == rootB) return;
            if (rank[rootA] < rank[rootB]) (rootA, rootB) = (rootB, rootA);
            parent[rootB] = rootA;
            if (rank[rootA] == rank[rootB]) rank[rootA]++;
        }
    }
}

public sealed record MatchOutput(IReadOnlyList<MatchCluster> Clusters);
public sealed record MatchCluster(IReadOnlyList<MatchedRecord> Records);
public sealed record MatchedRecord(int Index, SourceDealerRecord Record, double MatchConfidence, IReadOnlyList<MatchEvidence> Evidence);
public sealed record MatchEvidence(string LeftSource, string LeftKey, string RightSource, string RightKey, double Confidence, IReadOnlyList<string> Reasons);
public sealed record PairScore(bool IsMatch, double Confidence, MatchEvidence Evidence);
