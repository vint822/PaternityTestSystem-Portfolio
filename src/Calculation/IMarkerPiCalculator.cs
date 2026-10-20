using PaternityTestSystem.Portfolio.Models;

namespace PaternityTestSystem.Portfolio.Calculation
{
    /// <summary>
    /// Integration boundary only.
    /// The production PI calculation implementation is intentionally not distributed.
    /// </summary>
    public interface IMarkerPiCalculator
    {
        PaternityCalculationResult Calculate(
            string marker,
            Genotype child,
            Genotype allegedParent);
    }
}
