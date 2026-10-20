using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PaternityTestSystem.Portfolio.Storage
{
    public static class CaseFileStorageService
    {
        private const string InputFilesFolderName = "InputFiles";
        private const string ReportsFolderName = "Reports";
        private const string CalculationsFolderName = "Calculations";

        private static readonly string PortfolioCasesRoot =
            Path.Combine(
                AppContext.BaseDirectory,
                "PortfolioData",
                "Cases");

        public static string GetCasesRootPath()
        {
            return PortfolioCasesRoot;
        }

        public static string GetCaseFolderPath(string caseNo)
        {
            return GetCaseFolderPath(
                caseNo,
                GetCasesRootPath());
        }

        internal static string GetCaseFolderPath(
            string caseNo,
            string casesRoot)
        {
            var normalizedCaseNo =
                ValidateCaseNumberPathSegment(caseNo);

            string normalizedCasesRoot;
            string caseFolderPath;

            try
            {
                normalizedCasesRoot =
                    Path.TrimEndingDirectorySeparator(
                        Path.GetFullPath(casesRoot));

                caseFolderPath =
                    Path.GetFullPath(
                        Path.Combine(
                            normalizedCasesRoot,
                            normalizedCaseNo));
            }
            catch (Exception ex)
                when (ex is ArgumentException ||
                      ex is NotSupportedException ||
                      ex is PathTooLongException)
            {
                Debug.WriteLine(ex);

                throw new ArgumentException(
                    "案號無法用於案件資料夾。",
                    nameof(caseNo));
            }

            var relativePath =
                Path.GetRelativePath(
                    normalizedCasesRoot,
                    caseFolderPath);

            var parentPath =
                Path.GetDirectoryName(
                    caseFolderPath);

            var isDirectChild =
                !Path.IsPathRooted(relativePath) &&
                !string.Equals(
                    relativePath,
                    ".",
                    StringComparison.Ordinal) &&
                !ContainsDirectorySeparator(
                    relativePath) &&
                !string.IsNullOrWhiteSpace(
                    parentPath) &&
                string.Equals(
                    Path.TrimEndingDirectorySeparator(
                        Path.GetFullPath(parentPath)),
                    normalizedCasesRoot,
                    StringComparison.OrdinalIgnoreCase);

            if (!isDirectChild)
            {
                throw new ArgumentException(
                    "案號不可指向 CasesRoot 以外的資料夾。",
                    nameof(caseNo));
            }

            return caseFolderPath;
        }

        public static string GetInputFilesFolderPath(
            string caseNo)
        {
            return Path.Combine(
                GetCaseFolderPath(caseNo),
                InputFilesFolderName);
        }

        public static string GetInputFilesOperationFolderPath(
            string caseNo,
            string operationId)
        {
            return Path.Combine(
                GetInputFilesFolderPath(caseNo),
                SanitizePathPart(operationId));
        }

        public static string GetReportsFolderPath(
            string caseNo)
        {
            return Path.Combine(
                GetCaseFolderPath(caseNo),
                ReportsFolderName);
        }

        public static string GetCalculationsFolderPath(
            string caseNo)
        {
            return GetCalculationsFolderPath(
                caseNo,
                GetCasesRootPath());
        }

        internal static string GetCalculationsFolderPath(
            string caseNo,
            string casesRoot)
        {
            return Path.Combine(
                GetCaseFolderPath(
                    caseNo,
                    casesRoot),
                CalculationsFolderName);
        }

        public static string GetCalculationOperationFolderPath(
            string caseNo,
            string operationId)
        {
            return GetCalculationOperationFolderPath(
                caseNo,
                operationId,
                GetCasesRootPath());
        }

        internal static string GetCalculationOperationFolderPath(
            string caseNo,
            string operationId,
            string casesRoot)
        {
            if (string.IsNullOrWhiteSpace(operationId))
            {
                throw new ArgumentException(
                    "Operation ID is required.",
                    nameof(operationId));
            }

            return Path.Combine(
                GetCalculationsFolderPath(
                    caseNo,
                    casesRoot),
                SanitizePathPart(operationId));
        }

        public static void EnsureCaseFolders(
            string caseNo)
        {
            EnsureCaseFolders(
                caseNo,
                GetCasesRootPath());
        }

        internal static void EnsureCaseFolders(
            string caseNo,
            string casesRoot)
        {
            var caseFolderPath =
                GetCaseFolderPath(
                    caseNo,
                    casesRoot);

            Directory.CreateDirectory(
                Path.Combine(
                    caseFolderPath,
                    InputFilesFolderName));

            Directory.CreateDirectory(
                Path.Combine(
                    caseFolderPath,
                    ReportsFolderName));

            Directory.CreateDirectory(
                Path.Combine(
                    caseFolderPath,
                    CalculationsFolderName));
        }

        public static string CreateOperationId()
        {
            var timestamp =
                DateTime.Now.ToString(
                    "yyyyMMdd_HHmmss");

            var suffix =
                Guid.NewGuid()
                    .ToString("N")
                    .Substring(0, 4);

            return
                $"op_{timestamp}_{suffix}";
        }

        public static async Task<string>
            CopyInputFileToCaseFolderAsync(
                string caseNo,
                string sourceFilePath,
                string fileType)
        {
            if (string.IsNullOrWhiteSpace(
                    sourceFilePath))
            {
                throw new ArgumentException(
                    "Source file path is required.",
                    nameof(sourceFilePath));
            }

            if (!File.Exists(
                    sourceFilePath))
            {
                throw new FileNotFoundException(
                    "Input file does not exist.");
            }

            EnsureCaseFolders(caseNo);

            var originalFileName =
                Path.GetFileName(
                    sourceFilePath);

            var safeFileType =
                SanitizePathPart(
                    fileType);

            var safeFileName =
                SanitizeFileName(
                    originalFileName);

            var timestamp =
                DateTime.UtcNow.ToString(
                    "yyyyMMddHHmmssfff");

            var storedFileName =
                $"{safeFileType}_{timestamp}_{safeFileName}";

            var storedFilePath =
                Path.Combine(
                    GetInputFilesFolderPath(
                        caseNo),
                    storedFileName);

            await using (
                var source =
                    new FileStream(
                        sourceFilePath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read))
            await using (
                var destination =
                    new FileStream(
                        storedFilePath,
                        FileMode.CreateNew,
                        FileAccess.Write,
                        FileShare.None))
            {
                await source.CopyToAsync(
                    destination);
            }

            return storedFilePath;
        }

        public static async Task<string>
            CopyInputFileToCaseOperationFolderAsync(
                string caseNo,
                string operationId,
                string sourceFilePath,
                string storedFileName)
        {
            if (string.IsNullOrWhiteSpace(
                    sourceFilePath))
            {
                throw new ArgumentException(
                    "Source file path is required.",
                    nameof(sourceFilePath));
            }

            if (!File.Exists(
                    sourceFilePath))
            {
                throw new FileNotFoundException(
                    "Input file does not exist.");
            }

            if (string.IsNullOrWhiteSpace(
                    operationId))
            {
                throw new ArgumentException(
                    "Operation ID is required.",
                    nameof(operationId));
            }

            EnsureCaseFolders(caseNo);

            var operationFolder =
                GetInputFilesOperationFolderPath(
                    caseNo,
                    operationId);

            Directory.CreateDirectory(
                operationFolder);

            var storedFilePath =
                Path.Combine(
                    operationFolder,
                    SanitizeFileName(
                        storedFileName));

            await using (
                var source =
                    new FileStream(
                        sourceFilePath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read))
            await using (
                var destination =
                    new FileStream(
                        storedFilePath,
                        FileMode.CreateNew,
                        FileAccess.Write,
                        FileShare.None))
            {
                await source.CopyToAsync(
                    destination);
            }

            return storedFilePath;
        }

        public static bool TryDeleteInputFilesOperationFolder(
            string caseNo,
            string operationId,
            out string? warning)
        {
            warning = null;

            try
            {
                var inputRoot =
                    Path.GetFullPath(
                        GetInputFilesFolderPath(
                            caseNo));

                var operationFolder =
                    Path.GetFullPath(
                        GetInputFilesOperationFolderPath(
                            caseNo,
                            operationId));

                var requiredPrefix =
                    inputRoot.TrimEnd(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar) +
                    Path.DirectorySeparatorChar;

                if (!operationFolder.StartsWith(
                        requiredPrefix,
                        StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(
                        operationFolder,
                        inputRoot,
                        StringComparison.OrdinalIgnoreCase))
                {
                    warning =
                        "本次 CSV 資料夾路徑不安全，已略過自動清理。";

                    return false;
                }

                if (!Directory.Exists(
                        operationFolder))
                {
                    return true;
                }

                Directory.Delete(
                    operationFolder,
                    recursive: true);

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);

                warning =
                    "本次 CSV 資料夾清理失敗，可能有檔案正在使用中。";

                return false;
            }
        }

        public static string CreateReportFilePath(
            string caseNo,
            string documentName)
        {
            EnsureCaseFolders(caseNo);

            return Path.Combine(
                GetReportsFolderPath(caseNo),
                SanitizeFileName(
                    documentName));
        }

        public static string CreateCalculationWorksheetFilePath(
            string caseNo,
            string operationId,
            string documentName)
        {
            return CreateCalculationWorksheetFilePath(
                caseNo,
                operationId,
                documentName,
                GetCasesRootPath());
        }

        internal static string CreateCalculationWorksheetFilePath(
            string caseNo,
            string operationId,
            string documentName,
            string casesRoot)
        {
            EnsureCaseFolders(
                caseNo,
                casesRoot);

            var operationFolder =
                GetCalculationOperationFolderPath(
                    caseNo,
                    operationId,
                    casesRoot);

            Directory.CreateDirectory(
                operationFolder);

            return Path.Combine(
                operationFolder,
                SanitizeFileName(
                    documentName));
        }

        public static bool TryDeleteCalculationWorksheet(
            string caseNo,
            string operationId,
            string worksheetPath,
            out string? warning)
        {
            return TryDeleteCalculationWorksheet(
                caseNo,
                operationId,
                worksheetPath,
                GetCasesRootPath(),
                out warning);
        }

        internal static bool TryDeleteCalculationWorksheet(
            string caseNo,
            string operationId,
            string worksheetPath,
            string casesRoot,
            out string? warning)
        {
            warning = null;

            try
            {
                var operationFolder =
                    Path.TrimEndingDirectorySeparator(
                        Path.GetFullPath(
                            GetCalculationOperationFolderPath(
                                caseNo,
                                operationId,
                                casesRoot)));

                var fullWorksheetPath =
                    Path.GetFullPath(
                        worksheetPath);

                var relativePath =
                    Path.GetRelativePath(
                        operationFolder,
                        fullWorksheetPath);

                if (Path.IsPathRooted(
                        relativePath) ||
                    relativePath.StartsWith(
                        "..",
                        StringComparison.Ordinal) ||
                    relativePath.IndexOfAny(
                        new[]
                        {
                            Path.DirectorySeparatorChar,
                            Path.AltDirectorySeparatorChar
                        }) >= 0)
                {
                    warning =
                        "計算核對表清理範圍驗證失敗，未刪除任何檔案。";

                    return false;
                }

                if (File.Exists(
                        fullWorksheetPath))
                {
                    File.Delete(
                        fullWorksheetPath);
                }

                if (Directory.Exists(
                        operationFolder) &&
                    !Directory
                        .EnumerateFileSystemEntries(
                            operationFolder)
                        .Any())
                {
                    Directory.Delete(
                        operationFolder);
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);

                warning =
                    "本次計算核對表清理失敗，請人工確認檔案。";

                return false;
            }
        }

        public static bool TryDeleteReportFiles(
            string caseNo,
            IEnumerable<string> reportFilePaths,
            out string? warning)
        {
            warning = null;

            var cleanupFailed = false;

            try
            {
                var reportsRoot =
                    Path.GetFullPath(
                        GetReportsFolderPath(
                            caseNo));

                var requiredPrefix =
                    reportsRoot.TrimEnd(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar) +
                    Path.DirectorySeparatorChar;

                foreach (
                    var reportFilePath
                    in reportFilePaths
                        .Where(path =>
                            !string.IsNullOrWhiteSpace(
                                path))
                        .Distinct(
                            StringComparer.OrdinalIgnoreCase))
                {
                    try
                    {
                        var fullPath =
                            Path.GetFullPath(
                                reportFilePath);

                        if (!fullPath.StartsWith(
                                requiredPrefix,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            cleanupFailed = true;
                            continue;
                        }

                        if (File.Exists(
                                fullPath))
                        {
                            File.Delete(
                                fullPath);
                        }
                    }
                    catch
                    {
                        cleanupFailed = true;
                    }
                }
            }
            catch
            {
                cleanupFailed = true;
            }

            if (cleanupFailed)
            {
                warning =
                    "部分本次產生的 Word 報告無法清理，可能仍被其他程式使用。";

                return false;
            }

            return true;
        }

        private static string SanitizePathPart(
            string value)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                return "Unknown";
            }

            var invalidChars =
                Path.GetInvalidFileNameChars();

            var sanitized =
                new string(
                    value
                        .Trim()
                        .Select(ch =>
                            invalidChars.Contains(ch)
                                ? '_'
                                : ch)
                        .ToArray());

            return string.IsNullOrWhiteSpace(
                sanitized)
                ? "Unknown"
                : sanitized;
        }

        private static string ValidateCaseNumberPathSegment(
            string caseNo)
        {
            if (string.IsNullOrWhiteSpace(
                    caseNo))
            {
                throw new ArgumentException(
                    "案號不可為空白。",
                    nameof(caseNo));
            }

            var normalizedCaseNo =
                caseNo.Trim();

            if (string.Equals(
                    normalizedCaseNo,
                    ".",
                    StringComparison.Ordinal) ||
                string.Equals(
                    normalizedCaseNo,
                    "..",
                    StringComparison.Ordinal) ||
                Path.IsPathRooted(
                    normalizedCaseNo) ||
                ContainsDirectorySeparator(
                    normalizedCaseNo) ||
                normalizedCaseNo.IndexOfAny(
                    Path.GetInvalidFileNameChars()) >= 0)
            {
                throw new ArgumentException(
                    "案號不可包含路徑或無效的檔名字元。",
                    nameof(caseNo));
            }

            return normalizedCaseNo;
        }

        private static bool ContainsDirectorySeparator(
            string value)
        {
            return
                value.IndexOf(
                    Path.DirectorySeparatorChar) >= 0 ||
                value.IndexOf(
                    Path.AltDirectorySeparatorChar) >= 0 ||
                value.IndexOf('\\') >= 0 ||
                value.IndexOf('/') >= 0;
        }

        private static string SanitizeFileName(
            string fileName)
        {
            return SanitizePathPart(
                fileName);
        }
    }
}
