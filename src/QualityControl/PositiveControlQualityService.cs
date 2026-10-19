using PaternityTestSystem.Portfolio.Models;
using PaternityTestSystem.Portfolio.Parsing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace PaternityTestSystem.Portfolio.QualityControl
{
    /// <summary>
    /// Compares an actual Positive Control profile with an expected profile.
    /// The expected production dataset is intentionally not included
    /// in this portfolio.
    /// </summary>
    public static class PositiveControlQualityService
    {
        private const double AlleleTolerance = 0.000001;

        public static async Task<QualityControlSummary> ValidateAsync(
            string pcCsvPath,
            IEnumerable<MarkerGenotype> expectedMarkers)
        {
            var parseResult = await GenotypeCsvParser.ParseAsync(pcCsvPath);
            var summary = Validate(parseResult.Markers, expectedMarkers);

            summary.Warnings.AddRange(
                parseResult.Warnings.Select(
                    warning => $"PC CSV: {warning}"));

            summary.Errors.AddRange(
                parseResult.Errors.Select(
                    error => $"PC CSV: {error}"));

            if (summary.Errors.Count > 0)
            {
                summary.IsPassed = false;
            }

            return summary;
        }

        public static QualityControlSummary Validate(
            IEnumerable<MarkerGenotype> actualMarkers,
            IEnumerable<MarkerGenotype> expectedMarkers)
        {
            var expectedMarkerList =
                expectedMarkers?.ToList()
                ?? new List<MarkerGenotype>();

            var summary = new QualityControlSummary
            {
                TotalMarkerCount = expectedMarkerList.Count
            };

            if (expectedMarkerList.Count == 0)
            {
                summary.Errors.Add(
                    "Expected profile must contain at least one marker.");

                summary.IsPassed = false;
                return summary;
            }

            var actualByMarker =
                BuildActualMarkerLookup(
                    actualMarkers,
                    summary);

            var expectedMarkerNames =
                new HashSet<string>(
                    expectedMarkerList.Select(
                        expected => NormalizeMarker(expected.Marker)),
                    StringComparer.OrdinalIgnoreCase);

            if (expectedMarkerNames.Count != expectedMarkerList.Count)
            {
                summary.Errors.Add(
                    "Expected profile contains duplicate marker names.");
            }

            if (summary.Errors.Count > 0)
            {
                summary.IsPassed = false;
                return summary;
            }

            foreach (var expected in expectedMarkerList)
            {
                var normalizedMarker =
                    NormalizeMarker(expected.Marker);

                if (!actualByMarker.TryGetValue(
                        normalizedMarker,
                        out var actual))
                {
                    summary.MissingMarkerCount++;

                    summary.Results.Add(
                        new QualityControlMarkerResult
                        {
                            Marker = expected.Marker,
                            ExpectedAllele1 =
                                expected.Genotype.Allele1,
                            ExpectedAllele2 =
                                expected.Genotype.Allele2,
                            IsMatched = false,
                            Message =
                                "PC CSV is missing this marker."
                        });

                    continue;
                }

                if (GenotypesMatch(
                        expected.Genotype,
                        actual.Genotype))
                {
                    summary.MatchedCount++;

                    summary.Results.Add(
                        new QualityControlMarkerResult
                        {
                            Marker = expected.Marker,
                            ExpectedAllele1 =
                                expected.Genotype.Allele1,
                            ExpectedAllele2 =
                                expected.Genotype.Allele2,
                            ActualAllele1 =
                                actual.Genotype.Allele1,
                            ActualAllele2 =
                                actual.Genotype.Allele2,
                            IsMatched = true,
                            Message = "Matched"
                        });

                    continue;
                }

                summary.MismatchedCount++;

                summary.Results.Add(
                    new QualityControlMarkerResult
                    {
                        Marker = expected.Marker,
                        ExpectedAllele1 =
                            expected.Genotype.Allele1,
                        ExpectedAllele2 =
                            expected.Genotype.Allele2,
                        ActualAllele1 =
                            actual.Genotype.Allele1,
                        ActualAllele2 =
                            actual.Genotype.Allele2,
                        IsMatched = false,
                        Message =
                            $"expected {FormatGenotype(expected.Genotype)}, " +
                            $"actual {FormatGenotype(actual.Genotype)}"
                    });
            }

            AddExtraMarkerWarnings(
                actualByMarker.Values,
                expectedMarkerNames,
                summary);

            summary.IsPassed =
                summary.Errors.Count == 0 &&
                summary.MissingMarkerCount == 0 &&
                summary.MismatchedCount == 0;

            return summary;
        }

        private static Dictionary<string, MarkerGenotype>
            BuildActualMarkerLookup(
                IEnumerable<MarkerGenotype> actualMarkers,
                QualityControlSummary summary)
        {
            var actualByMarker =
                new Dictionary<string, MarkerGenotype>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (var actual in actualMarkers)
            {
                if (string.IsNullOrWhiteSpace(actual.Marker))
                {
                    continue;
                }

                var normalizedMarker =
                    NormalizeMarker(actual.Marker);

                if (actualByMarker.ContainsKey(
                        normalizedMarker))
                {
                    summary.Warnings.Add(
                        $"PC CSV marker '{actual.Marker}' appears more than once after marker normalization.");

                    continue;
                }

                actualByMarker[normalizedMarker] = actual;
            }

            return actualByMarker;
        }

        private static void AddExtraMarkerWarnings(
            IEnumerable<MarkerGenotype> actualMarkers,
            ISet<string> expectedMarkerNames,
            QualityControlSummary summary)
        {
            foreach (var actual in actualMarkers)
            {
                if (expectedMarkerNames.Contains(
                        NormalizeMarker(actual.Marker)))
                {
                    continue;
                }

                var message =
                    $"PC CSV contains an extra marker: {actual.Marker}";

                summary.Warnings.Add(message);

                summary.Results.Add(
                    new QualityControlMarkerResult
                    {
                        Marker = actual.Marker,
                        ActualAllele1 =
                            actual.Genotype.Allele1,
                        ActualAllele2 =
                            actual.Genotype.Allele2,
                        IsMatched = true,
                        IsWarning = true,
                        Message = message
                    });
            }
        }

        private static bool GenotypesMatch(
            Genotype expected,
            Genotype actual)
        {
            return AllelesMatch(
                       expected.Allele1,
                       actual.Allele1) &&
                   AllelesMatch(
                       expected.Allele2,
                       actual.Allele2);
        }

        private static bool AllelesMatch(
            double expected,
            double actual)
        {
            return Math.Abs(
                       expected - actual)
                   <= AlleleTolerance;
        }

        private static string FormatGenotype(
            Genotype genotype)
        {
            return
                $"{FormatAllele(genotype.Allele1)}, " +
                $"{FormatAllele(genotype.Allele2)}";
        }

        private static string FormatAllele(
            double allele)
        {
            return allele.ToString(
                "0.###",
                CultureInfo.InvariantCulture);
        }

        private static string NormalizeMarker(
            string marker)
        {
            return (marker ?? string.Empty)
                .Trim()
                .Replace("_", " ");
        }
    }
}
