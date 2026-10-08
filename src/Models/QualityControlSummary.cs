namespace PaternityTestSystem.Portfolio.Models;

public sealed class QualityControlSummary
{
    public List<QualityControlMarkerResult> Results { get; } = [];
    public List<string> Warnings { get; } = [];
    public List<string> Errors { get; } = [];
    public int TotalMarkerCount { get; internal set; }
    public int MatchedCount => Results.Count(r => r.Status == QualityControlMarkerStatus.Matched);
    public int MissingMarkerCount => Results.Count(r => r.Status == QualityControlMarkerStatus.Missing);
    public int MismatchedCount => Results.Count(r => r.Status == QualityControlMarkerStatus.Mismatched);
    public bool IsPassed => Errors.Count == 0 && TotalMarkerCount > 0 &&
                            MatchedCount == TotalMarkerCount;
    public string Status => IsPassed ? "PASS" : "FAIL";
}
