using System.Diagnostics;

namespace PaternityTestSystem.Portfolio.Storage;

/// <summary>
/// Stores files under a caller-selected root. The application must control that root
/// and prevent concurrent filesystem changes by untrusted users. Link checks are
/// defense in depth, not an atomic filesystem sandbox. Do not share one operation
/// between concurrent writers or run compensation while a writer is active.
/// </summary>
public sealed class CaseFileStorageService
{
    private static readonly string[] Categories = ["InputFiles", "Reports", "Calculations"];
    private readonly string root;

    public CaseFileStorageService(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
            throw new ArgumentException("A storage root is required.", nameof(rootPath));

        try
        {
            root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(rootPath));
            RejectLinks(root);
        }
        catch (Exception ex) when (IsStorageError(ex))
        {
            Debug.WriteLine(ex);
            throw new ArgumentException("The storage root is not available.", nameof(rootPath));
        }
    }

    public string GetCaseFolderPath(string caseId)
    {
        var identifier = ValidateCaseId(caseId);
        try { return DirectChild(root, identifier); }
        catch (Exception ex) when (IsStorageError(ex))
        {
            Debug.WriteLine(ex);
            throw new IOException("The case folder path is not available.");
        }
    }

    /// <summary>
    /// Creates fresh GUID folders in all three categories. Case folders and earlier
    /// operations are never owned by the returned handle and are never cleaned up.
    /// </summary>
    public StorageOperation BeginOperation(string caseId)
    {
        var operation = new StorageOperation(this, ValidateCaseId(caseId));
        var created = new List<string>();
        try
        {
            foreach (var category in Categories)
            {
                var categoryFolder = DirectChild(GetCaseFolderPath(operation.CaseId), category);
                EnsureDirectory(categoryFolder);
                var folder = DirectChild(categoryFolder, operation.Id);
                RejectLinks(folder);
                if (GetAttributesIfPresent(folder) is not null)
                    throw new IOException("The operation folder already exists.");

                Directory.CreateDirectory(folder);
                created.Add(folder);
                RejectLinks(folder);
            }

            return operation;
        }
        catch (Exception ex) when (IsStorageError(ex))
        {
            Debug.WriteLine(ex);
            var cleaned = TryRemoveFolders(created);
            throw new IOException(cleaned
                ? "The file operation could not be started."
                : "The file operation could not be started; temporary file cleanup needs attention.");
        }
    }

    /// <summary>
    /// Copies without modifying the source or overwriting an existing destination.
    /// A failed or cancelled copy compensates this entire operation. Callers must
    /// also compensate when a later report or database step fails.
    /// </summary>
    public async Task<string> CopyInputFileAsync(
        StorageOperation operation,
        string sourcePath,
        string storedFileName,
        CancellationToken cancellationToken = default)
    {
        ValidateOperation(operation);
        try
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
                throw new ArgumentException("An input file is required.");

            var fullSourcePath = Path.GetFullPath(sourcePath);
            RejectLinks(fullSourcePath);
            var destinationPath = OutputPath(operation, "InputFiles", storedFileName);
            await using (var source = new FileStream(
                fullSourcePath, FileMode.Open, FileAccess.Read, FileShare.Read,
                81920, FileOptions.Asynchronous | FileOptions.SequentialScan))
            await using (var destination = new FileStream(
                destinationPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                81920, FileOptions.Asynchronous))
            {
                await source.CopyToAsync(destination, cancellationToken);
            }

            return destinationPath;
        }
        catch (Exception ex) when (IsStorageError(ex) || ex is OperationCanceledException)
        {
            Debug.WriteLine(ex);
            var cleaned = TryCompensate(operation, out _);
            if (ex is OperationCanceledException && cleaned)
                throw new OperationCanceledException("Input copying was cancelled.", cancellationToken);

            throw new IOException(cleaned
                ? "The input file could not be copied."
                : "The input file could not be copied; temporary file cleanup needs attention.");
        }
    }

    // The caller writes the returned paths with CreateNew, closes all handles, and
    // compensates the operation if its file/database workflow cannot complete.
    public string GetReportFilePath(StorageOperation operation, string fileName) =>
        OutputPath(operation, "Reports", fileName);

    public string GetCalculationFilePath(StorageOperation operation, string fileName) =>
        OutputPath(operation, "Calculations", fileName);

    /// <summary>
    /// Removes only this handle's operation folders. On false, keep the handle and
    /// retry after addressing the warning. Even a partial cleanup closes the
    /// operation to further writes; a new attempt needs a new operation.
    /// </summary>
    public bool TryCompensate(StorageOperation operation, out string? warning)
    {
        warning = null;
        if (operation is null || !ReferenceEquals(operation.Owner, this))
        {
            warning = "The file operation does not belong to this storage service.";
            return false;
        }

        operation.Closed = true;
        var cleaned = TryRemoveFolders(Categories.Select(category => OperationFolder(operation, category)));
        if (!cleaned)
            warning = "Some temporary files could not be removed; cleanup needs attention.";
        return cleaned;
    }

    public static string SanitizeFileName(string? fileName)
    {
        var sanitized = new string((fileName ?? string.Empty).Trim()
            .Select(ch => char.IsControl(ch) || "<>:\"/\\|?*".Contains(ch) ? '_' : ch)
            .Take(120).ToArray()).TrimEnd(' ', '.');
        if (string.IsNullOrWhiteSpace(sanitized))
            sanitized = "file";

        var stem = sanitized.Split('.')[0].TrimEnd(' ').ToUpperInvariant();
        if (stem is "CON" or "PRN" or "AUX" or "NUL" or "CONIN$" or "CONOUT$" ||
            (stem.Length == 4 && (stem.StartsWith("COM", StringComparison.Ordinal) ||
                                  stem.StartsWith("LPT", StringComparison.Ordinal)) &&
             "123456789¹²³".Contains(stem[3])))
            sanitized = "_" + sanitized;
        return sanitized;
    }

    private string OutputPath(StorageOperation operation, string category, string fileName)
    {
        ValidateOperation(operation);
        try
        {
            var folder = OperationFolder(operation, category);
            RejectLinks(folder);
            if (!Directory.Exists(folder))
                throw new IOException("The operation folder is unavailable.");
            var output = DirectChild(folder, SanitizeFileName(fileName));
            RejectLinks(output);
            if (GetAttributesIfPresent(output) is not null)
                throw new IOException("An output file already exists.");
            return output;
        }
        catch (Exception ex) when (IsStorageError(ex))
        {
            Debug.WriteLine(ex);
            throw new IOException("An output path could not be prepared.");
        }
    }

    private string OperationFolder(StorageOperation operation, string category) =>
        DirectChild(DirectChild(GetCaseFolderPath(operation.CaseId), category), operation.Id);

    private void ValidateOperation(StorageOperation operation)
    {
        if (operation is null || !ReferenceEquals(operation.Owner, this) || operation.Closed)
            throw new ArgumentException("An active operation from this storage service is required.", nameof(operation));
    }

    private static string ValidateCaseId(string caseId)
    {
        // A deliberately narrow identifier avoids separators, drive syntax, device
        // names, trailing-dot aliases and alternate data streams across platforms.
        if (string.IsNullOrWhiteSpace(caseId) || caseId.Length > 80 ||
            !caseId.All(ch => char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_') ||
            !string.Equals(caseId, SanitizeFileName(caseId), StringComparison.Ordinal))
            throw new ArgumentException("The case identifier is invalid.", nameof(caseId));
        return caseId;
    }

    private static string DirectChild(string parent, string name)
    {
        var child = Path.GetFullPath(Path.Combine(parent, name));
        var relative = Path.GetRelativePath(parent, child);
        if (Path.IsPathRooted(relative) || relative is "." or ".." ||
            relative.Contains('/') || relative.Contains('\\'))
            throw new IOException("A storage path is outside its allowed folder.");
        return child;
    }

    private static void EnsureDirectory(string path)
    {
        RejectLinks(path);
        Directory.CreateDirectory(path);
        RejectLinks(path);
    }

    private static void RejectLinks(string path)
    {
        for (var current = Path.GetFullPath(path); current is not null; current = Path.GetDirectoryName(current))
        {
            if (GetAttributesIfPresent(current) is { } attributes &&
                (attributes & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Linked storage paths are not allowed.");
        }
    }

    private static FileAttributes? GetAttributesIfPresent(string path)
    {
        try { return File.GetAttributes(path); }
        catch (FileNotFoundException) { return null; }
        catch (DirectoryNotFoundException) { return null; }
    }

    private static bool TryRemoveFolders(IEnumerable<string> folders)
    {
        var success = true;
        foreach (var folder in folders)
        {
            try { DeleteOwnedTree(folder); }
            catch (Exception ex) when (IsStorageError(ex))
            {
                Debug.WriteLine(ex);
                success = false;
            }
        }
        return success;
    }

    private static void DeleteOwnedTree(string folder)
    {
        RejectLinks(folder);
        var attributes = GetAttributesIfPresent(folder);
        if (attributes is null)
            return;
        if ((attributes.Value & FileAttributes.Directory) == 0)
            throw new IOException("An operation folder is not a directory.");

        foreach (var entry in Directory.EnumerateFileSystemEntries(folder))
        {
            var child = DirectChild(folder, Path.GetFileName(entry));
            RejectLinks(child);
            var childAttributes = File.GetAttributes(child);
            if ((childAttributes & FileAttributes.Directory) != 0)
                DeleteOwnedTree(child);
            else
                File.Delete(child);
        }

        RejectLinks(folder);
        Directory.Delete(folder, recursive: false);
    }

    private static bool IsStorageError(Exception ex) =>
        ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException;
}

/// <summary>An in-process handle for one operation; never reconstruct it from user input.</summary>
public sealed class StorageOperation
{
    internal StorageOperation(CaseFileStorageService owner, string caseId)
    {
        Owner = owner;
        CaseId = caseId;
    }

    internal CaseFileStorageService Owner { get; }
    internal bool Closed { get; set; }
    public string CaseId { get; }
    public string Id { get; } = Guid.NewGuid().ToString("N");
}
