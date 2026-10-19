namespace PaternityTestSystem.Portfolio.Models
{
    public class QualityControlMarkerResult
    {
        public string Marker { get; set; }
            = string.Empty;

        public double? ExpectedAllele1 { get; set; }
        public double? ExpectedAllele2 { get; set; }
        public double? ActualAllele1 { get; set; }
        public double? ActualAllele2 { get; set; }

        public bool IsMatched { get; set; }
        public bool IsWarning { get; set; }

        public string Message { get; set; }
            = string.Empty;
    }
}
