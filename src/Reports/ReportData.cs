using System;
using System.Collections.Generic;

namespace PaternityTestSystem.Portfolio.Reports
{
    /// <summary>
    /// Generic document input for the portfolio example.
    /// This is intentionally not the production clinical report schema.
    /// </summary>
    public class ReportData
    {
        public string CaseNo { get; set; } = string.Empty;
        public DateTime ReportDate { get; set; }
        public string Participant1 { get; set; } = string.Empty;
        public string Participant2 { get; set; } = string.Empty;

        public List<MarkerReportRow> MarkerRows { get; set; }
            = new List<MarkerReportRow>();
    }

    public class MarkerReportRow
    {
        public string Marker { get; set; } = string.Empty;

        public List<string> Participant1Alleles { get; set; }
            = new List<string>();

        public List<string> Participant2Alleles { get; set; }
            = new List<string>();
    }
}
