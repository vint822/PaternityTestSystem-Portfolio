namespace PaternityTestSystem.Portfolio.Models;

/// <summary>A normalized, immutable pair of finite, non-negative allele values.</summary>
public sealed record Genotype
{
    public double Allele1 { get; }
    public double Allele2 { get; }

    public Genotype(double allele1, double allele2)
    {
        if (!double.IsFinite(allele1) || allele1 < 0)
            throw new ArgumentOutOfRangeException(nameof(allele1));
        if (!double.IsFinite(allele2) || allele2 < 0)
            throw new ArgumentOutOfRangeException(nameof(allele2));

        Allele1 = Math.Min(allele1, allele2);
        Allele2 = Math.Max(allele1, allele2);
    }
}
