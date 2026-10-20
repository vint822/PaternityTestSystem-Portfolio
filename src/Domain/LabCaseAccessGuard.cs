using System;

namespace PaternityTestSystem.Portfolio.Domain
{
    public static class LabCaseAccessGuard
    {
        public const string CancelledStatus = "Cancelled";

        public static bool IsCancelled(string? status)
        {
            return string.Equals(
                status,
                CancelledStatus,
                StringComparison.OrdinalIgnoreCase);
        }

        public static void EnsureCanOpenDocument(string? status)
        {
            EnsureNotCancelled(
                status,
                "此案件已取消，無法開啟報告。");
        }

        public static void EnsureCanGenerateReports(string? status)
        {
            EnsureNotCancelled(
                status,
                "此案件已取消，無法產生報告。");
        }

        public static void EnsureCanCalculate(string? status)
        {
            EnsureNotCancelled(
                status,
                "案件已取消，不可重新計算。");
        }

        public static void EnsureCanGenerateCalculationWorksheet(string? status)
        {
            EnsureNotCancelled(
                status,
                "案件已取消，不可產生計算核對表。");
        }

        public static void EnsureCanOpenCalculationWorksheet(string? status)
        {
            EnsureNotCancelled(
                status,
                "案件已取消，不可開啟計算核對表。");
        }

        public static void EnsureNotCancelled(
            string? status,
            string message)
        {
            if (IsCancelled(status))
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
