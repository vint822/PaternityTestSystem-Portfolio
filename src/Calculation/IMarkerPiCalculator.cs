using PaternityTestSystem.Portfolio.Models;

namespace PaternityTestSystem.Portfolio.Calculation;

/// <summary>Integration boundary only. No calculation implementation is distributed.</summary>
public interface IMarkerPiCalculator
{
    PaternityCalculationResult Calculate(string marker, Genotype child, Genotype allegedParent);
}
