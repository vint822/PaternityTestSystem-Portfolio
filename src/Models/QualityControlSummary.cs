using System.Collections.Generic;

namespace PaternityTestSystem.Portfolio.Models
{
    public class QualityControlSummary
    {
        public bool IsPassed { get; set; }
        public int TotalMarkerCount { get; set; }
        public int MatchedCount { get; set; }
        public int MismatchedCount { get; set; }
        public int MissingMarkerCount { get; set; }

        public List<QualityControlMarkerResult> Results { get; set; }
            = new List<QualityControlMarkerResult>();

        public List<string> Warnings { get; set; }
            = new List<string>();

        public List<string> Errors { get; set; }
            = new List<string>();
    }
}
