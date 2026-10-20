using PaternityTestSystem.Portfolio.Parsing;
using Xunit;

namespace PaternityTestSystem.Portfolio.Tests;

public sealed class GenotypeCsvParserTests
{
    [Fact]
    public async Task ParseAsync_GroupsMarkerNamesAndNormalizesAlleles()
    {
        // Arrange: fictional markers, reordered headers, and a single-allele entry.
        var csvPath = Path.Combine(
            Path.GetTempPath(),
            $"portfolio-{Guid.NewGuid():N}.csv");

        await File.WriteAllTextAsync(
            csvPath,
            "Allele,mArKeR\n12,MarkerA\n10,markera\n9.3,MarkerB\n");

        try
        {
            // Act
            var result = await GenotypeCsvParser.ParseAsync(csvPath);

            // Assert
            Assert.Empty(result.Errors);
            Assert.Equal(2, result.Markers.Count);

            Assert.Equal(10, result.Markers[0].Genotype.Allele1);
            Assert.Equal(12, result.Markers[0].Genotype.Allele2);

            Assert.Equal(9.3, result.Markers[1].Genotype.Allele1);
            Assert.Equal(9.3, result.Markers[1].Genotype.Allele2);
        }
        finally
        {
            File.Delete(csvPath);
        }
    }
}
