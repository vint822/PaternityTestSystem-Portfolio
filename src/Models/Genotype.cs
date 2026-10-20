namespace PaternityTestSystem.Portfolio.Models
{
    /// <summary>
    /// Represents a two-allele genotype for one STR marker.
    /// </summary>
    public class Genotype
    {
        public double Allele1 { get; }
        public double Allele2 { get; }

        public Genotype(double allele1, double allele2)
        {
            if (allele1 <= allele2)
            {
                Allele1 = allele1;
                Allele2 = allele2;
            }
            else
            {
                Allele1 = allele2;
                Allele2 = allele1;
            }
        }
    }
}
