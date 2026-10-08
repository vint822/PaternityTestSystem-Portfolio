using System.Diagnostics;
using System.Globalization;
using Microsoft.VisualBasic.FileIO;
using PaternityTestSystem.Portfolio.Models;

namespace PaternityTestSystem.Portfolio.Parsing;

public sealed class GenotypeCsvParseResult
{
    public List<MarkerGenotype> Markers { get; } = [];
    public List<string> Warnings { get; } = [];
    public List<string> Errors { get; } = [];
    public bool HasErrors => Errors.Count > 0;
}

public static class GenotypeCsvParser
{
    public static Task<GenotypeCsvParseResult> ParseAsync(
        string filePath,
        CancellationToken cancellationToken = default) =>
        Task.Run(() => Parse(filePath, cancellationToken), cancellationToken);

    private static GenotypeCsvParseResult Parse(string filePath, CancellationToken cancellationToken)
    {
        var result = new GenotypeCsvParseResult();
        if (string.IsNullOrWhiteSpace(filePath))
        {
            result.Errors.Add("CSV file path is empty.");
            return result;
        }

        if (!File.Exists(filePath))
        {
            result.Errors.Add("CSV file does not exist.");
            return result;
        }

        var alleleMap = new Dictionary<string, List<double>>(StringComparer.OrdinalIgnoreCase);
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
            if (headers is null)
            {
                result.Errors.Add("CSV header row is missing.");
                return result;
            }

            var headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < headers.Length; index++)
            {
                if (!headerIndex.TryAdd(headers[index].Trim(), index))
                    result.Errors.Add("CSV header contains a duplicate column name.");
            }

            if (!headerIndex.TryGetValue("Marker", out var markerIndex))
                result.Errors.Add("CSV header is missing required column: Marker.");
            if (!headerIndex.TryGetValue("Allele", out var alleleIndex))
                result.Errors.Add("CSV header is missing required column: Allele.");
            if (result.HasErrors)
                return result;

            while (!parser.EndOfData)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var rowNumber = parser.LineNumber;
                var fields = parser.ReadFields();
                if (fields is null)
                    continue;

                var marker = GetField(fields, markerIndex).Trim();
                var alleleText = GetField(fields, alleleIndex).Trim();
                if (marker.Length == 0 && alleleText.Length == 0)
                {
                    result.Warnings.Add($"Row {rowNumber}: empty marker and allele were skipped.");
                    continue;
                }

                if (marker.Length == 0 || alleleText.Length == 0)
                {
                    result.Errors.Add($"Row {rowNumber}: both marker and allele are required.");
                    continue;
                }

                if (!double.TryParse(alleleText, NumberStyles.Float, CultureInfo.InvariantCulture,
                        out var allele) || !double.IsFinite(allele) || allele < 0)
                {
                    // Do not silently turn a partly invalid genotype into a homozygous result.
                    result.Errors.Add($"Row {rowNumber}: allele must be a finite, non-negative number.");
                    continue;
                }

                if (!alleleMap.TryGetValue(marker, out var alleles))
                {
                    alleles = [];
                    alleleMap.Add(marker, alleles);
                    markerOrder.Add(marker);
                }

                alleles.Add(allele);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (MalformedLineException ex)
        {
            Debug.WriteLine(ex);
            result.Errors.Add($"CSV format is invalid at line {ex.LineNumber}.");
            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            result.Errors.Add("CSV could not be read. Please check the file and try again.");
            return result;
        }

        foreach (var marker in markerOrder)
        {
            var alleles = alleleMap[marker];
            if (alleles.Count > 2)
            {
                result.Errors.Add($"Marker '{marker}' has more than two alleles.");
                continue;
            }

            // A single observed allele is represented as a homozygous pair.
            var secondAllele = alleles.Count == 1 ? alleles[0] : alleles[1];
            result.Markers.Add(new MarkerGenotype(marker, new Genotype(alleles[0], secondAllele)));
        }

        if (result.Markers.Count == 0 && !result.HasErrors)
            result.Errors.Add("CSV contains no genotype data.");

        return result;
    }

    private static string GetField(string[] fields, int index) =>
        index >= 0 && index < fields.Length ? fields[index] : string.Empty;
}
