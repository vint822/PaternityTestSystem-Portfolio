using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using PaternityTestSystem.Portfolio.Models;

namespace PaternityTestSystem.Portfolio.Reports;

/// <summary>
/// Demonstrates editing a caller-supplied DOCX copy. Only word/document.xml is edited;
/// headers, footers, text boxes and clinical report rules are outside this example.
/// No template is distributed with the portfolio.
/// </summary>
public static class WordTemplateService
{
    private static readonly XNamespace W =
        "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

    /// <summary>
    /// Fills four generic fields and clones a three-cell marker row containing
    /// {{MARKER}}, {{GENOTYPE_1}} and {{GENOTYPE_2}}, respectively.
    /// Each genotype cell must contain only its placeholder in one paragraph.
    /// Input text cannot contain the reserved placeholder opening sequence {{.
    /// Work on a disposable document copy: saving is not an atomic file operation.
    /// </summary>
    public static void FillReport(string documentPath, ReportData data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentPath);
        ArgumentNullException.ThrowIfNull(data);
        ValidateData(data);

        using var archive = ZipFile.Open(documentPath, ZipArchiveMode.Update);
        var entry = archive.GetEntry("word/document.xml")
            ?? throw new InvalidDataException("The document body is missing.");

        XDocument document;
        using (var stream = entry.Open())
        using (var reader = XmlReader.Create(stream, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = 5_000_000
        }))
        {
            document = XDocument.Load(reader, LoadOptions.PreserveWhitespace);
        }

        BuildMarkerRows(document, data.MarkerRows);
        var replacements = new Dictionary<string, string>
        {
            ["{{CASE_NO}}"] = data.CaseNo,
            ["{{REPORT_DATE}}"] = data.ReportDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["{{PARTICIPANT_1}}"] = data.Participant1,
            ["{{PARTICIPANT_2}}"] = data.Participant2
        };

        foreach (var paragraph in document.Descendants(W + "p"))
        {
            ReplaceAcrossRuns(paragraph, replacements);
        }

        if (GetText(document.Root!).Contains("{{", StringComparison.Ordinal))
        {
            throw new InvalidDataException("The document contains an unresolved placeholder.");
        }

        // Validate and serialize the full XML before changing the ZIP entry.
        using var xmlBuffer = new MemoryStream();
        using (var writer = new StreamWriter(xmlBuffer, new UTF8Encoding(false), leaveOpen: true))
        {
            document.Save(writer, SaveOptions.DisableFormatting);
        }

        entry.Delete();
        var replacement = archive.CreateEntry("word/document.xml", CompressionLevel.Optimal);
        using var output = replacement.Open();
        xmlBuffer.Position = 0;
        xmlBuffer.CopyTo(output);
    }

    private static void ValidateData(ReportData data)
    {
        static bool IsValidText(string? value) =>
            value is not null && !value.Contains("{{", StringComparison.Ordinal);

        if (!IsValidText(data.CaseNo) || !IsValidText(data.Participant1) ||
            !IsValidText(data.Participant2) || data.MarkerRows is null ||
            data.MarkerRows.Any(row => row is null || !IsValidText(row.Marker) ||
                row.Participant1 is null || row.Participant2 is null))
        {
            throw new ArgumentException("The report input is incomplete or contains reserved text.", nameof(data));
        }
    }

    private static void BuildMarkerRows(XDocument document, IReadOnlyList<MarkerReportRow> rows)
    {
        var templates = document.Descendants(W + "tr")
            .Where(row => GetText(row).Contains("{{MARKER}}", StringComparison.Ordinal))
            .ToList();
        if (templates.Count != 1)
        {
            throw new InvalidDataException("Exactly one generic marker row is required.");
        }

        var template = templates[0];
        if (template.Elements(W + "tc").Count() != 3)
        {
            throw new InvalidDataException("The generic marker row must have three cells.");
        }

        foreach (var item in rows)
        {
            var row = new XElement(template);
            FillGenotypeCell(row, "{{GENOTYPE_1}}", item.Participant1, item.Participant2);
            FillGenotypeCell(row, "{{GENOTYPE_2}}", item.Participant2, item.Participant1);
            var replacement = new Dictionary<string, string> { ["{{MARKER}}"] = item.Marker };
            foreach (var paragraph in row.Descendants(W + "p"))
            {
                ReplaceAcrossRuns(paragraph, replacement);
            }

            template.AddBeforeSelf(row);
        }

        template.Remove();
    }

    private static void FillGenotypeCell(XElement row, string placeholder, Genotype value, Genotype other)
    {
        var cells = row.Elements(W + "tc")
            .Where(cell => GetText(cell) == placeholder)
            .ToList();
        if (cells.Count != 1 || cells[0].Elements(W + "p").Count() != 1)
        {
            throw new InvalidDataException("Each genotype cell must contain only its placeholder.");
        }

        var paragraph = cells[0].Element(W + "p")!;
        // Preserve paragraph and run style while replacing only this demonstration cell.
        var style = paragraph.Descendants(W + "rPr").FirstOrDefault();
        var properties = style is null ? new XElement(W + "rPr") : new XElement(style);
        paragraph.Elements().Where(element => element.Name != W + "pPr").Remove();

        var alleles = new[] { value.Allele1, value.Allele2 };
        for (var index = 0; index < alleles.Length; index++)
        {
            if (index > 0)
            {
                paragraph.Add(CreateRun(", ", bold: false, properties));
            }

            var allele = alleles[index];
            var shared = allele == other.Allele1 || allele == other.Allele2;
            paragraph.Add(CreateRun(allele.ToString(CultureInfo.InvariantCulture), shared, properties));
        }
    }

    private static XElement CreateRun(string text, bool bold, XElement templateProperties)
    {
        var properties = new XElement(templateProperties);
        properties.Elements(W + "b").Remove();
        properties.Elements(W + "bCs").Remove();
        // An explicit false overrides bold inherited from a paragraph style.
        properties.Add(new XElement(W + "b", new XAttribute(W + "val", bold ? "1" : "0")));
        return new XElement(W + "r", properties,
            new XElement(W + "t", new XAttribute(XNamespace.Xml + "space", "preserve"), text));
    }

    private static void ReplaceAcrossRuns(XElement paragraph, IReadOnlyDictionary<string, string> replacements)
    {
        var texts = paragraph.Descendants(W + "t").ToList();
        var combined = string.Concat(texts.Select(text => text.Value));
        var matches = Regex.Matches(combined, @"\{\{[A-Z0-9_]+\}\}").Cast<Match>().ToList();

        // Work from right to left so earlier offsets remain valid. Inserted values
        // are never scanned again, avoiding recursive replacement of input text.
        foreach (var match in matches.AsEnumerable().Reverse())
        {
            if (!replacements.TryGetValue(match.Value, out var value))
            {
                continue;
            }

            var start = FindPosition(texts, match.Index);
            var end = FindPosition(texts, match.Index + match.Length - 1);
            var prefix = texts[start.Index].Value[..start.Offset];
            var suffix = texts[end.Index].Value[(end.Offset + 1)..];
            texts[start.Index].Value = prefix + value + (start.Index == end.Index ? suffix : "");
            texts[start.Index].SetAttributeValue(XNamespace.Xml + "space", "preserve");
            for (var index = start.Index + 1; index < end.Index; index++)
            {
                texts[index].Value = "";
            }

            if (end.Index != start.Index)
            {
                texts[end.Index].Value = suffix;
                texts[end.Index].SetAttributeValue(XNamespace.Xml + "space", "preserve");
            }
        }
    }

    private static (int Index, int Offset) FindPosition(IReadOnlyList<XElement> texts, int offset)
    {
        for (var index = 0; index < texts.Count; index++)
        {
            if (offset < texts[index].Value.Length)
            {
                return (index, offset);
            }

            offset -= texts[index].Value.Length;
        }

        throw new InvalidDataException("The placeholder position is invalid.");
    }

    private static string GetText(XElement element) =>
        string.Concat(element.Descendants(W + "t").Select(text => text.Value));
}
