using System.Text;

namespace LF.WebApi.Common;

// Minimal RFC 4180 CSV builder. Uses ';' as the delimiter and a UTF-8 BOM so the report opens
// cleanly in Russian-locale Excel (which treats ';' as the list separator).
public static class CsvWriter
{
    private const char Delimiter = ';';

    public static byte[] ToCsvBytes(IReadOnlyList<string> header, IEnumerable<IReadOnlyList<string>> rows)
    {
        var builder = new StringBuilder();
        AppendRow(builder, header);

        foreach (var row in rows)
        {
            AppendRow(builder, row);
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(builder.ToString())).ToArray();
    }

    private static void AppendRow(StringBuilder builder, IReadOnlyList<string> fields)
    {
        for (var i = 0; i < fields.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(Delimiter);
            }

            builder.Append(Escape(fields[i]));
        }

        builder.Append("\r\n");
    }

    private static string Escape(string? field)
    {
        var value = field ?? string.Empty;

        if (value.Contains('"') || value.Contains(Delimiter) || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
