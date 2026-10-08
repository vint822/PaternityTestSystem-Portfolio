using PaternityTestSystem.Portfolio.Domain;
using Xunit;

namespace PaternityTestSystem.Portfolio.Tests;

public sealed class LabCaseAccessGuardTests
{
    [Fact]
    public void CancelledCase_BlocksCalculationAndRestrictedDocuments()
    {
        // Arrange
        const string status = " cancelled ";
        Action[] restrictedActions =
        [
            () => LabCaseAccessGuard.EnsureCanCalculate(status),
            () => LabCaseAccessGuard.EnsureCanGenerateReports(status),
            () => LabCaseAccessGuard.EnsureCanOpenDocument(status),
            () => LabCaseAccessGuard.EnsureCanGenerateCalculationWorksheet(status),
            () => LabCaseAccessGuard.EnsureCanOpenCalculationWorksheet(status)
        ];

        // Act
        var exceptions = restrictedActions.Select(action => Record.Exception(action)).ToArray();

        // Assert
        Assert.True(LabCaseAccessGuard.IsCancelled(status));
        Assert.All(exceptions, exception => Assert.IsType<InvalidOperationException>(exception));
    }
}
