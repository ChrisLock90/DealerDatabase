namespace DealerDatabase.Import.Importing;

using System.Text;

/// <summary>
/// Small RFC-4180 compatible reader for the supplied CSV exports.
/// Avoids coupling the task solution to a third-party CSV package.
/// </summary>
public static class CsvReader
{
    public static IReadOnlyList<Dictionary<string, string>> Read(string path)
    {
        using var reader = new StreamReader(path, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var rows = ParseRecords(reader).ToList();
        if (rows.Count == 0) return [];

        var headers = rows[0].Select(x => x.TrimStart('\uFEFF')).ToArray();
        var result = new List<Dictionary<string, string>>(Math.Max(0, rows.Count - 1));
        foreach (var row in rows.Skip(1))
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers.Length; i++)
                dict[headers[i]] = i < row.Count ? row[i] : string.Empty;
            result.Add(dict);
        }
        return result;
    }

    private static IEnumerable<List<string>> ParseRecords(TextReader reader)
    {
        var row = new List<string>();
        var field = new StringBuilder();
        var quoted = false;

        while (reader.Read() is var code && code != -1)
        {
            var c = (char)code;
            if (quoted)
            {
                if (c == '"')
                {
                    if (reader.Peek() == '"')
                    {
                        reader.Read();
                        field.Append('"');
                    }
                    else
                    {
                        quoted = false;
                    }
                }
                else
                {
                    field.Append(c);
                }
                continue;
            }

            switch (c)
            {
                case '"' when field.Length == 0:
                    quoted = true;
                    break;
                case ',':
                    row.Add(field.ToString());
                    field.Clear();
                    break;
                case '\r':
                    if (reader.Peek() == '\n') reader.Read();
                    row.Add(field.ToString());
                    field.Clear();
                    yield return row;
                    row = [];
                    break;
                case '\n':
                    row.Add(field.ToString());
                    field.Clear();
                    yield return row;
                    row = [];
                    break;
                default:
                    field.Append(c);
                    break;
            }
        }

        if (quoted)
            throw new InvalidDataException("Unexpected end of file inside a quoted CSV field.");

        if (field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString());
            yield return row;
        }
    }
}
