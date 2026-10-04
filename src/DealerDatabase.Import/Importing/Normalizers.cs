using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace DealerDatabase.Import.Importing;

public static partial class Normalizers
{
    private static readonly string[] LegalSuffixes =
    [
        "LIMITED", "LTD", "PLC", "LLP", "INC", "INCORPORATED", "CO", "COMPANY"
    ];

    public static string NormalizeName(string? value)
    {
        var text = Normalize(value);
        foreach (var suffix in LegalSuffixes.Select(Normalize))
        {
            if (text.EndsWith(suffix, StringComparison.Ordinal) && text.Length > suffix.Length)
            {
                text = text[..^suffix.Length];
                break;
            }
        }

        return text;
    }

    public static string NormalizeLoose(string? value) => Normalize(value);

    public static string NormalizeCompanyNumber(string? value)
    {
        var normalized = Normalize(value);
        return normalized.All(char.IsDigit) && normalized.Length is > 0 and < 8
            ? normalized.PadLeft(8, '0')
            : normalized;
    }

    public static string NormalizeVatNumber(string? value)
    {
        var text = Normalize(value);
        return text.StartsWith("GB", StringComparison.Ordinal) ? text[2..] : text;
    }

    public static string NormalizeFcaNumber(string? value) => Digits(value);

    public static string NormalizePostcode(string? value) => Normalize(value);

    public static string NormalizePhone(string? value)
    {
        var digits = Digits(value);
        if (digits.StartsWith("00", StringComparison.Ordinal))
            digits = digits[2..];

        if (digits.StartsWith("0", StringComparison.Ordinal))
            digits = "44" + digits[1..];

        return digits;
    }

    public static string NormalizeEmail(string? value) => value?.Trim().ToLowerInvariant() ?? string.Empty;

    public static string NormalizeDomain(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Contains('@', StringComparison.Ordinal))
            return string.Empty;

        var candidate = value.Trim();
        if (!candidate.Contains("://", StringComparison.Ordinal))
            candidate = "https://" + candidate;

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
            return string.Empty;

        var host = uri.Host.ToLowerInvariant();
        if (host.StartsWith("www.", StringComparison.Ordinal))
            host = host[4..];

        return host;
    }

    public static string CleanWebsite(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value.Trim();
    }

    public static DateTime? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var formats = new[]
        {
            "yyyy-MM-dd", "yyyy-MM-ddTHH:mm:ssZ", "yyyy-MM-ddTHH:mm:ssK",
            "dd/MM/yyyy", "d/MM/yyyy", "d MMM yyyy", "dd MMM yyyy",
            "dd MMMM yyyy", "d MMMM yyyy"
        };

        if (DateTime.TryParseExact(value.Trim(), formats, CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal, out var exact))
            return exact.Date;

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal, out var parsed))
            return parsed.Date;

        return null;
    }

    public static int? ParseInt(string? value)
        => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;

    public static decimal? ParseDecimal(string? value)
        => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;

    public static bool? ParseBool(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value.Trim().ToLowerInvariant() switch
        {
            "true" or "yes" or "y" or "1" => true,
            "false" or "no" or "n" or "0" => false,
            _ => null
        };
    }

    public static string[] SplitValues(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return [];
        return value
            .Split([';', ',', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static string Digits(string? value)
        => string.Concat((value ?? string.Empty).Where(char.IsDigit));

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var normalized = value.Normalize(NormalizationForm.FormKC).ToLowerInvariant();
        return string.Concat(normalized.Where(char.IsLetterOrDigit));
    }
}
