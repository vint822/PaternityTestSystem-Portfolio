namespace PaternityTestSystem.Portfolio.Models;

public enum QualityControlMarkerStatus
{
    Matched,
    Missing,
    Mismatched,
    Extra
}

public sealed record QualityControlMarkerResult(
    string Marker,
    Genotype? Expected,
    Genotype? Actual,
    QualityControlMarkerStatus Status);
