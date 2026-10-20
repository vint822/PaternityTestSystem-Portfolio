using Microsoft.VisualBasic.FileIO;
using PaternityTestSystem.Portfolio.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PaternityTestSystem.Portfolio.Parsing
{
    public class GenotypeCsvParseResult
    {
        public List<MarkerGenotype> Markers { get; set; }
            = new List<MarkerGenotype>();

        public List<string> Warnings { get; set; }
            = new List<string>();

        public List<string> Errors { get; set; }
            = new List<string>();

        public bool HasErrors => Errors.Count > 0;
    }

    public static class GenotypeCsvParser
    {
        /// <summary>
        /// Parses a genotype CSV file without blocking the WPF UI thread.
        /// </summary>
        public static Task<GenotypeCsvParseResult> ParseAsync(string filePath)
        {
            return Task.Run(() => Parse(filePath));
        }

        private static GenotypeCsvParseResult Parse(string filePath)
        {
            var result = new GenotypeCsvParseResult();

            if (string.IsNullOrWhiteSpace(filePath))
            {
                result.Errors.Add("CSV file path is empty.");
                return result;
            }

            if (!File.Exists(filePath))
            {
                Debug.WriteLine($"CSV file not found: {filePath}");

                result.Errors.Add("CSV file does not exist.");
                return result;
            }

            var alleleMap =
                new Dictionary<string, List<double>>(
                    StringComparer.OrdinalIgnoreCase);

            var markerOrder = new List<string>();

            try
            {
                using var parser = new TextFieldParser(filePath);

                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");
                parser.HasFieldsEnclosedInQuotes = true;

                if (parser.EndOfData)
                {
                    result.Errors.Add("CSV file is empty.");
                    return result;
                }

                var headers = parser.ReadFields();

                if (headers == null)
                {
                    result.Errors.Add("CSV header row is missing.");
                    return result;
                }

                var headerIndex = headers
                    .Select((header, index) => new
                    {
                        Header = header.Trim(),
                        Index = index
                    })
                    .ToDictionary(
                        item => item.Header,
                        item => item.Index,
                        StringComparer.OrdinalIgnoreCase);

                if (!headerIndex.TryGetValue(
                        "Marker",
                        out var markerIndex))
                {
                    result.Errors.Add(
                        "CSV header is missing required column: Marker.");

                    return result;
                }

                if (!headerIndex.TryGetValue(
                        "Allele",
                        out var alleleIndex))
                {
                    result.Errors.Add(
                        "CSV header is missing required column: Allele.");

                    return result;
                }

                var rowNumber = 1;

                while (!parser.EndOfData)
                {
                    rowNumber++;

                    var fields = parser.ReadFields();

                    if (fields == null)
                    {
                        continue;
                    }

                    var marker =
                        GetField(fields, markerIndex).Trim();

                    var alleleText =
                        GetField(fields, alleleIndex).Trim();

                    if (string.IsNullOrWhiteSpace(marker) ||
                        string.IsNullOrWhiteSpace(alleleText))
                    {
                        continue;
                    }

                    if (!double.TryParse(
                            alleleText,
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out var allele))
                    {
                        result.Warnings.Add(
                            $"Row {rowNumber}: allele '{alleleText}' " +
                            $"for marker '{marker}' could not be parsed.");

                        continue;
                    }

                    if (!alleleMap.TryGetValue(
                            marker,
                            out var alleles))
                    {
                        alleles = new List<double>();

                        alleleMap[marker] = alleles;
                        markerOrder.Add(marker);
                    }

                    alleles.Add(allele);
                }
            }
            catch (MalformedLineException ex)
            {
                result.Errors.Add(
                    $"CSV format error near row {ex.LineNumber}.");

                Debug.WriteLine(ex);

                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);

                result.Errors.Add(
                    "Unexpected error occurred while parsing CSV.");

                return result;
            }

            foreach (var marker in markerOrder)
            {
                var alleles = alleleMap[marker];

                // Normalize homozygous markers so every genotype
                // is represented by two allele values.
                if (alleles.Count == 1)
                {
                    alleles.Add(alleles[0]);
                }
                else if (alleles.Count > 2)
                {
                    result.Errors.Add(
                        $"Marker '{marker}' has {alleles.Count} alleles; " +
                        "expected one or two.");

                    continue;
                }

                result.Markers.Add(
                    new MarkerGenotype
                    {
                        Marker = marker,
                        Genotype = new Genotype(
                            alleles[0],
                            alleles[1])
                    });
            }

            return result;
        }

        private static string GetField(
            string[] fields,
            int index)
        {
            return index >= 0 && index < fields.Length
                ? fields[index]
                : string.Empty;
        }
    }
}
