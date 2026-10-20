namespace PaternityTestSystem.Portfolio.Models
{
    /// <summary>
    /// Genotype observed at a named STR marker.
    /// </summary>
    public class MarkerGenotype
    {
        public string Marker { get; set; } = string.Empty;

        public Genotype Genotype { get; set; }
            = new Genotype(0, 0);
    }
}
