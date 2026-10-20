namespace PaternityTestSystem.Portfolio.Models
{
    /// <summary>
    /// Per-marker calculation result.
    /// Production frequency terms, formula descriptions and calculation traces
    /// are intentionally not included in the public portfolio.
    /// </summary>
    public class PaternityCalculationResult
    {
        public string Marker { get; set; } = string.Empty;
        public double Pi { get; set; }
        public double CumulativePi { get; set; }
        public bool IsExcluded { get; set; }
    }
}
