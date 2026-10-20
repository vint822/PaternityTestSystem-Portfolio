using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace PaternityTestSystem.Portfolio.Reports
{
    /// <summary>
    /// Demonstrates DOCX placeholder replacement and dynamic marker-row generation.
    /// Production templates and clinical report rules are intentionally not included.
    /// </summary>
    public static class WordTemplateService
    {
        private static readonly XNamespace W =
            "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

        public static void FillReport(
            string documentPath,
            ReportData data)
        {
            using var archive =
                ZipFile.Open(
                    documentPath,
                    ZipArchiveMode.Update);

            var entry =
                archive.GetEntry(
                    "word/document.xml")
                ?? throw new InvalidDataException(
                    "Word template is missing word/document.xml.");

            XDocument document;

            using (var stream = entry.Open())
            {
                document =
                    XDocument.Load(
                        stream,
                        LoadOptions.PreserveWhitespace);
            }

            BuildMarkerRows(
                document,
                data.MarkerRows);

            var replacements =
                new Dictionary<string, string>
                {
                    ["{{CASE_NO}}"] =
                        data.CaseNo,

                    ["{{REPORT_DATE}}"] =
                        data.ReportDate.ToString(
                            "yyyy-MM-dd",
                            CultureInfo.InvariantCulture),

                    ["{{PARTICIPANT_1}}"] =
                        data.Participant1,

                    ["{{PARTICIPANT_2}}"] =
                        data.Participant2
                };

            foreach (
                var paragraph
                in document.Descendants(
                    W + "p"))
            {
                ReplaceTextAcrossRuns(
                    paragraph,
                    replacements);
            }

            var remaining =
                string.Concat(
                    document
                        .Descendants(W + "t")
                        .Select(
                            element =>
                                element.Value));

            if (remaining.Contains(
                    "{{",
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    "Word report still contains an unresolved placeholder.");
            }

            entry.Delete();

            var replacementEntry =
                archive.CreateEntry(
                    "word/document.xml",
                    CompressionLevel.Optimal);

            using var output =
                replacementEntry.Open();

            using var writer =
                new StreamWriter(
                    output,
                    new UTF8Encoding(false));

            document.Save(
                writer,
                SaveOptions.DisableFormatting);
        }

        private static void BuildMarkerRows(
            XDocument document,
            IReadOnlyList<MarkerReportRow> markerRows)
        {
            var templateRow =
                document
                    .Descendants(W + "tr")
                    .FirstOrDefault(
                        row =>
                            GetText(row)
                                .Contains(
                                    "{{MARKER}}",
                                    StringComparison.Ordinal))
                ?? throw new InvalidDataException(
                    "Word template does not contain a marker row template.");

            foreach (var markerRow in markerRows)
            {
                var row =
                    new XElement(
                        templateRow);

                var rowReplacements =
                    new Dictionary<string, string>
                    {
                        ["{{MARKER}}"] =
                            markerRow.Marker
                    };

                foreach (
                    var paragraph
                    in row.Descendants(
                        W + "p"))
                {
                    ReplaceTextAcrossRuns(
                        paragraph,
                        rowReplacements);
                }

                FillGenotypeCell(
                    row,
                    "{{GENOTYPE_1}}",
                    markerRow.Participant1Alleles,
                    markerRow.Participant2Alleles);

                FillGenotypeCell(
                    row,
                    "{{GENOTYPE_2}}",
                    markerRow.Participant2Alleles,
                    markerRow.Participant1Alleles);

                templateRow.AddBeforeSelf(
                    row);
            }

            templateRow.Remove();
        }

        private static void FillGenotypeCell(
            XElement row,
            string placeholder,
            IReadOnlyList<string> alleles,
            IReadOnlyList<string> otherAlleles)
        {
            var cell =
                row.Descendants(
                        W + "tc")
                    .FirstOrDefault(
                        candidate =>
                            GetText(candidate)
                                .Contains(
                                    placeholder,
                                    StringComparison.Ordinal))
                ?? throw new InvalidDataException(
                    $"Marker row does not contain {placeholder}.");

            var paragraph =
                cell.Descendants(
                        W + "p")
                    .First();

            var templateRun =
                paragraph
                    .Descendants(W + "r")
                    .FirstOrDefault();

            var runProperties =
                templateRun?
                    .Element(W + "rPr");

            paragraph
                .Elements(W + "r")
                .Remove();

            var shared =
                SharedAlleleService
                    .GetSharedAlleles(
                        alleles,
                        otherAlleles);

            for (
                var index = 0;
                index < alleles.Count;
                index++)
            {
                if (index > 0)
                {
                    paragraph.Add(
                        CreateRun(
                            ", ",
                            false,
                            runProperties));
                }

                var allele =
                    alleles[index].Trim();

                paragraph.Add(
                    CreateRun(
                        allele,
                        shared.Contains(
                            allele.Trim()),
                        runProperties));
            }
        }

        private static XElement CreateRun(
            string text,
            bool bold,
            XElement? templateRunProperties)
        {
            var run =
                new XElement(
                    W + "r");

            var properties =
                templateRunProperties == null
                    ? new XElement(
                        W + "rPr")
                    : new XElement(
                        templateRunProperties);

            properties
                .Elements(W + "b")
                .Remove();

            properties
                .Elements(W + "bCs")
                .Remove();

            if (bold)
            {
                properties.Add(
                    new XElement(
                        W + "b"));

                properties.Add(
                    new XElement(
                        W + "bCs"));
            }

            if (properties.HasElements)
            {
                run.Add(properties);
            }

            run.Add(
                new XElement(
                    W + "t",
                    new XAttribute(
                        XNamespace.Xml + "space",
                        "preserve"),
                    text));

            return run;
        }

        private static void ReplaceTextAcrossRuns(
            XElement container,
            IReadOnlyDictionary<string, string> replacements)
        {
            foreach (
                var replacement
                in replacements)
            {
                ReplacePlaceholder(
                    container,
                    replacement.Key,
                    replacement.Value
                        ?? string.Empty);
            }
        }

        private static void ReplacePlaceholder(
            XElement container,
            string placeholder,
            string value)
        {
            while (true)
            {
                var texts =
                    container
                        .Descendants(W + "t")
                        .ToList();

                var combined =
                    string.Concat(
                        texts.Select(
                            text =>
                                text.Value));

                var placeholderStart =
                    combined.IndexOf(
                        placeholder,
                        StringComparison.Ordinal);

                if (placeholderStart < 0)
                {
                    return;
                }

                var placeholderEnd =
                    placeholderStart +
                    placeholder.Length;

                var position = 0;
                var firstIndex = -1;
                var lastIndex = -1;
                var firstOffset = 0;
                var lastOffset = 0;

                for (
                    var index = 0;
                    index < texts.Count;
                    index++)
                {
                    var nextPosition =
                        position +
                        texts[index]
                            .Value.Length;

                    if (firstIndex < 0 &&
                        placeholderStart <
                        nextPosition)
                    {
                        firstIndex = index;

                        firstOffset =
                            placeholderStart -
                            position;
                    }

                    if (placeholderEnd <=
                        nextPosition)
                    {
                        lastIndex = index;

                        lastOffset =
                            placeholderEnd -
                            position;

                        break;
                    }

                    position =
                        nextPosition;
                }

                if (firstIndex < 0 ||
                    lastIndex < 0)
                {
                    throw new InvalidDataException(
                        $"Unable to replace placeholder: {placeholder}");
                }

                var prefix =
                    texts[firstIndex]
                        .Value.Substring(
                            0,
                            firstOffset);

                var suffix =
                    texts[lastIndex]
                        .Value.Substring(
                            lastOffset);

                texts[firstIndex].Value =
                    prefix +
                    value +
                    (firstIndex == lastIndex
                        ? suffix
                        : string.Empty);

                for (
                    var index =
                        firstIndex + 1;
                    index < lastIndex;
                    index++)
                {
                    texts[index].Value =
                        string.Empty;
                }

                if (lastIndex >
                    firstIndex)
                {
                    texts[lastIndex].Value =
                        suffix;
                }
            }
        }

        private static string GetText(
            XElement element)
        {
            return string.Concat(
                element
                    .Descendants(W + "t")
                    .Select(
                        text =>
                            text.Value));
        }
    }
}
