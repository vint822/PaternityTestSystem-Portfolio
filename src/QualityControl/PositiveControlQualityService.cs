using PaternityTestSystem.Portfolio.Models;

namespace PaternityTestSystem.Portfolio.QualityControl;

/// <summary>Compares supplied profiles. This assembly contains no expected control dataset.</summary>
public static class PositiveControlQualityService
{
    private const double AlleleTolerance = 0.000001;

    public static QualityControlSummary Validate(
        IEnumerable<MarkerGenotype> actualMarkers,
        IEnumerable<MarkerGenotype> expectedMarkers)
    {
        var summary = new QualityControlSummary();
        var actualByMarker = BuildLookup(actualMarkers, "Actual", summary);
        var expectedByMarker = BuildLookup(expectedMarkers, "Expected", summary);
        summary.TotalMarkerCount = expectedByMarker.Count;

        if (expectedByMarker.Count == 0)
            summary.Errors.Add("Expected profile must contain at least one marker.");
        if (summary.Errors.Count > 0)
            return summary;

        foreach (var (marker, expected) in expectedByMarker)
        {
            if (!actualByMarker.TryGetValue(marker, out var actual))
            {
                summary.Results.Add(new QualityControlMarkerResult(
                    marker, expected.Genotype, null, QualityControlMarkerStatus.Missing));
                continue;
            }

            var status = GenotypesMatch(expected.Genotype, actual.Genotype)
                ? QualityControlMarkerStatus.Matched
                : QualityControlMarkerStatus.Mismatched;
            summary.Results.Add(new QualityControlMarkerResult(
                marker, expected.Genotype, actual.Genotype, status));
        }

        foreach (var (marker, actual) in actualByMarker)
        {
            if (expectedByMarker.ContainsKey(marker))
                continue;

            summary.Warnings.Add($"Actual profile contains an extra marker: {marker}.");
            summary.Results.Add(new QualityControlMarkerResult(
                marker, null, actual.Genotype, QualityControlMarkerStatus.Extra));
        }

        return summary;
    }

    private static Dictionary<string, MarkerGenotype> BuildLookup(
        IEnumerable<MarkerGenotype>? markers,
        string source,
        QualityControlSummary summary)
    {
        var lookup = new Dictionary<string, MarkerGenotype>(StringComparer.OrdinalIgnoreCase);
        if (markers is null)
        {
            summary.Errors.Add($"{source} profile is required.");
            return lookup;
        }

        foreach (var item in markers)
        {
            if (item is null || string.IsNullOrWhiteSpace(item.Marker) || item.Genotype is null)
            {
                summary.Errors.Add($"{source} profile contains an incomplete marker.");
                continue;
            }

            if (!lookup.TryAdd(item.Marker.Trim(), item))
                summary.Errors.Add($"{source} profile contains duplicate marker names.");
        }

        return lookup;
    }

    private static bool GenotypesMatch(Genotype expected, Genotype actual) =>
        Math.Abs(expected.Allele1 - actual.Allele1) <= AlleleTolerance &&
        Math.Abs(expected.Allele2 - actual.Allele2) <= AlleleTolerance;
}
