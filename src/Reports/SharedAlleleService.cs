using System;
using System.Collections.Generic;
using System.Linq;

namespace PaternityTestSystem.Portfolio.Reports
{
    public static class SharedAlleleService
    {
        public static HashSet<string> GetSharedAlleles(
            IEnumerable<string> alleles1,
            IEnumerable<string> alleles2)
        {
            if (alleles1 == null)
            {
                throw new ArgumentNullException(nameof(alleles1));
            }

            if (alleles2 == null)
            {
                throw new ArgumentNullException(nameof(alleles2));
            }

            return alleles1
                .Select(NormalizeAllele)
                .Intersect(
                    alleles2.Select(NormalizeAllele),
                    StringComparer.Ordinal)
                .ToHashSet(
                    StringComparer.Ordinal);
        }

        private static string NormalizeAllele(
            string allele)
        {
            return allele.Trim();
        }
    }
}
