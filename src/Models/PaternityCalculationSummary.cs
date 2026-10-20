using System.Collections.Generic;

namespace PaternityTestSystem.Portfolio.Models
{
    /// <summary>
    /// Aggregate calculation summary.
    /// </summary>
    public class PaternityCalculationSummary
    {
        public List<PaternityCalculationResult> Results { get; set; }
            = new List<PaternityCalculationResult>();

        public List<string> Warnings { get; set; }
            = new List<string>();

        public List<string> Errors { get; set; }
            = new List<string>();

        public double Cpi { get; set; }
        public int CalculatedMarkerCount { get; set; }
        public int ExcludedMarkerCount { get; set; }

        public bool HasErrors => Errors.Count > 0;
    }
}
