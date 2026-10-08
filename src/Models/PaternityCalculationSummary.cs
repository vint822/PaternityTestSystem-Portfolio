namespace PaternityTestSystem.Portfolio.Models;

public sealed class PaternityCalculationSummary
{
    public List<PaternityCalculationResult> Results { get; } = [];
    public List<string> Warnings { get; } = [];
    public List<string> Errors { get; } = [];

    // A failed or incomplete operation must not expose a successful-looking total.
    public double? Cpi { get; internal set; }
    public int CalculatedMarkerCount => Results.Count;
    public bool HasErrors => Errors.Count > 0;
}
