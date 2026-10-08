namespace PaternityTestSystem.Portfolio.Domain;

/// <summary>Service-level rules apply even when an action bypasses the user interface.</summary>
public static class LabCaseAccessGuard
{
    public const string CancelledStatus = "Cancelled";

    public static bool IsCancelled(string? status) =>
        string.Equals(status?.Trim(), CancelledStatus, StringComparison.OrdinalIgnoreCase);

    public static void EnsureCanOpenDocument(string? status) =>
        EnsureNotCancelled(status, "Reports cannot be opened for a cancelled case.");

    public static void EnsureCanGenerateReports(string? status) =>
        EnsureNotCancelled(status, "Reports cannot be generated for a cancelled case.");

    public static void EnsureCanCalculate(string? status) =>
        EnsureNotCancelled(status, "A cancelled case cannot be recalculated.");

    public static void EnsureCanGenerateCalculationWorksheet(string? status) =>
        EnsureNotCancelled(status, "Calculation worksheets cannot be generated for a cancelled case.");

    public static void EnsureCanOpenCalculationWorksheet(string? status) =>
        EnsureNotCancelled(status, "Calculation worksheets cannot be opened for a cancelled case.");

    public static void EnsureNotCancelled(string? status, string message)
    {
        if (IsCancelled(status))
            throw new InvalidOperationException(message);
    }
}
