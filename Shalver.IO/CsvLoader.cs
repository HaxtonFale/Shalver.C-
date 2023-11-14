using Microsoft.VisualBasic.FileIO;
using System.Reflection;

namespace Shalver.IO;

public class CsvLoader
{
    private readonly string _namespace;
    private readonly Assembly _assembly;

    public CsvLoader(string @namespace, Assembly assembly)
    {
        _namespace = @namespace;
        _assembly = assembly;
    }

    public IEnumerable<string[]> LoadCsv(string csvName, bool hasHeader)
    {
        var filename = $"{_namespace}.{csvName}.csv";
        using var stream = _assembly.GetManifestResourceStream(filename) ?? throw new ArgumentException($"Could not load embedded resource matching {csvName}.csv", nameof(csvName));
        var parser = new TextFieldParser(stream) {
            CommentTokens = Array.Empty<string>(),
            Delimiters = new[]
            { ","
            },
            FieldWidths = Array.Empty<int>(),
            HasFieldsEnclosedInQuotes = true,
            TextFieldType = FieldType.Delimited,
            TrimWhiteSpace = false
        };
        if (hasHeader)
        {
            parser.ReadFields();
        }

        while (!parser.EndOfData)
        {
            yield return parser.ReadFields() ?? throw new InvalidOperationException("Failed to read a CSV row.");
        }
    }
}