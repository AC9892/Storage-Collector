using System.Text.Json;
using System.IO;
using SafeStorageScanner.Models;

namespace SafeStorageScanner.Services;

public sealed class SafetyLogService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    public string LogDirectory { get; private set; } = "";

    public void Configure(bool portableMode)
    {
        LogDirectory = portableMode
            ? Path.Combine(AppContext.BaseDirectory, "SafetyLogs")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SafeStorageScanner", "SafetyLogs");

        Directory.CreateDirectory(LogDirectory);
    }

    public string RecordScan(string root, IEnumerable<FileSystemScanItem> files, IEnumerable<FileSystemScanItem> folders, IEnumerable<DuplicateGroup> duplicates)
    {
        if (string.IsNullOrWhiteSpace(LogDirectory))
        {
            Configure(portableMode: true);
        }

        var file = Path.Combine(LogDirectory, $"scan-{DateTime.Now:yyyyMMdd-HHmmss}.json");
        var payload = new
        {
            scannedAt = DateTime.Now,
            root,
            mode = "dry-run",
            warning = "No files were deleted. Recommendations are conservative and must be reviewed before any future deletion feature is enabled.",
            files = files.Take(5000),
            folders = folders.Take(2000),
            recommendations = files.Concat(folders).Where(i => i.Category != DeletionCategory.DoNotDelete).Take(5000),
            duplicates = duplicates.Take(1000)
        };
        File.WriteAllText(file, JsonSerializer.Serialize(payload, JsonOptions));
        return file;
    }

    public string RecordAction(string action, string target, long bytes)
    {
        if (string.IsNullOrWhiteSpace(LogDirectory))
        {
            Configure(portableMode: true);
        }

        var file = Path.Combine(LogDirectory, $"action-{DateTime.Now:yyyyMMdd-HHmmss}.json");
        File.WriteAllText(file, JsonSerializer.Serialize(new
        {
            at = DateTime.Now,
            action,
            target,
            bytes,
            mode = "user-confirmed or dry-run",
            rollback = "Move actions can be rolled back manually by moving files from the destination back to the original relative path. Delete/uninstall actions are not implemented."
        }, JsonOptions));
        return file;
    }
}
