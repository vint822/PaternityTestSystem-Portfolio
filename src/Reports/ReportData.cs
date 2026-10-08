using PaternityTestSystem.Portfolio.Models;

namespace PaternityTestSystem.Portfolio.Reports;

/// <summary>Generic document input; this is not a clinical report schema.</summary>
public sealed record ReportData(
    string CaseNo,
    DateOnly ReportDate,
    string Participant1,
    string Participant2,
    IReadOnlyList<MarkerReportRow> MarkerRows);

public sealed record MarkerReportRow(
    string Marker,
    Genotype Participant1,
    Genotype Participant2);
