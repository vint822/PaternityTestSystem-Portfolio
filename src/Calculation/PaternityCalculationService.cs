using System.Diagnostics;
using PaternityTestSystem.Portfolio.Models;
using PaternityTestSystem.Portfolio.Parsing;

namespace PaternityTestSystem.Portfolio.Calculation;

/// <summary>Demonstrates parsing, matching, dependency injection, and aggregation only.</summary>
public sealed class PaternityCalculationService
{
    private readonly IMarkerPiCalculator _calculator;

    public PaternityCalculationService(IMarkerPiCalculator calculator) =>
        _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));

    public async Task<PaternityCalculationSummary> CalculateAsync(
        string parentCsvPath,
        string childCsvPath,
        CancellationToken cancellationToken = default)
    {
        var summary = new PaternityCalculationSummary();
        var parent = await GenotypeCsvParser.ParseAsync(parentCsvPath, cancellationToken);
        var child = await GenotypeCsvParser.ParseAsync(childCsvPath, cancellationToken);
        AddMessages(summary.Warnings, "Parent CSV", parent.Warnings);
        AddMessages(summary.Errors, "Parent CSV", parent.Errors);
        AddMessages(summary.Warnings, "Child CSV", child.Warnings);
        AddMessages(summary.Errors, "Child CSV", child.Errors);
        if (summary.HasErrors)
            return summary;

        var parentByMarker = parent.Markers.ToDictionary(m => m.Marker, StringComparer.OrdinalIgnoreCase);
        var childByMarker = child.Markers.ToDictionary(m => m.Marker, StringComparer.OrdinalIgnoreCase);
        foreach (var marker in childByMarker.Keys.Except(parentByMarker.Keys, StringComparer.OrdinalIgnoreCase))
            summary.Errors.Add($"Parent CSV is missing marker '{marker}'.");
        foreach (var marker in parentByMarker.Keys.Except(childByMarker.Keys, StringComparer.OrdinalIgnoreCase))
            summary.Errors.Add($"Child CSV is missing marker '{marker}'.");
        if (summary.HasErrors)
            return summary;

        foreach (var childMarker in child.Markers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var result = _calculator.Calculate(
                    childMarker.Marker, childMarker.Genotype, parentByMarker[childMarker.Marker].Genotype);

                if (result is null ||
                    !string.Equals(result.Marker, childMarker.Marker, StringComparison.OrdinalIgnoreCase) ||
                    !double.IsFinite(result.Pi) || result.Pi < 0)
                {
                    summary.Errors.Add("Calculator returned an invalid marker result.");
                    return summary;
                }

                summary.Results.Add(result);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                summary.Errors.Add("Marker calculation failed. No aggregate result is available.");
                return summary;
            }
        }

        var markerPis = summary.Results.Select(result => result.Pi).ToArray();
        var cpi = markerPis.Aggregate(1.0, (total, pi) => total * pi);
        if (!double.IsFinite(cpi) || (cpi == 0 && markerPis.All(pi => pi > 0)))
        {
            summary.Errors.Add("Aggregate result is outside the supported numeric range.");
            return summary;
        }

        summary.Cpi = cpi;
        return summary;
    }

    private static void AddMessages(List<string> target, string source, IEnumerable<string> messages) =>
        target.AddRange(messages.Select(message => $"{source}: {message}"));
}
