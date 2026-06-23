using SafeStorageScanner.Models;

namespace SafeStorageScanner.Services;

public sealed class SafetyClassifier
{
    private static readonly string[] ProtectedFragments =
    [
        @"\windows\",
        @"\windows",
        @"\program files\",
        @"\program files (x86)\",
        @"\system32\",
        @"\syswow64\",
        @"\drivers\",
        @"\appdata\",
        @"\programdata\microsoft\windows\",
        @"\$recycle.bin\"
    ];

    private static readonly HashSet<string> UsuallySafeExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".tmp", ".temp", ".log", ".dmp", ".old", ".bak", ".zip", ".7z", ".rar", ".iso"
    };

    private static readonly HashSet<string> NeverSuggestExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".sys", ".dll", ".drv", ".ocx", ".msi", ".msp", ".exe", ".com", ".bat", ".cmd", ".ps1"
    };

    public (DeletionCategory Category, string Explanation) ClassifyFile(string path, string extension, long sizeBytes, DateTime? lastAccessed)
    {
        var normalized = Normalize(path);
        if (ProtectedFragments.Any(normalized.Contains))
        {
            return (DeletionCategory.DoNotDelete, "Protected location. Windows, Program Files, drivers, AppData, and app dependency paths are not deletion candidates.");
        }

        if (NeverSuggestExtensions.Contains(extension))
        {
            return (DeletionCategory.DoNotDelete, "Executable, library, installer, script, driver, or system component. Do not delete directly.");
        }

        if (UsuallySafeExtensions.Contains(extension))
        {
            return (DeletionCategory.ReviewManually, "This file type is often temporary, a dump, backup, or archive. Review ownership and whether you still need it.");
        }

        if (lastAccessed.HasValue && lastAccessed.Value < DateTime.Now.AddMonths(-12) && sizeBytes >= 500L * 1024 * 1024)
        {
            return (DeletionCategory.ReviewManually, "Large file not accessed in over a year. Review before deleting; dry-run mode only records the recommendation.");
        }

        return (DeletionCategory.ReviewManually, "No clear safe-delete signal. Review manually and keep it if it belongs to an installed app or active project.");
    }

    public (string Action, int Confidence, long RecoverableBytes) ScoreFile(string path, string extension, long sizeBytes, DateTime? lastAccessed, int ageDays)
    {
        var normalized = Normalize(path);
        if (ProtectedFragments.Any(normalized.Contains) || NeverSuggestExtensions.Contains(extension))
        {
            return ("Do not modify", 98, 0);
        }

        if (IsCacheOrTempPath(normalized) || UsuallySafeExtensions.Contains(extension))
        {
            return ("Review cleanup", 82, sizeBytes);
        }

        if (IsOldInstaller(extension, path))
        {
            return ("Review old installer", 78, sizeBytes);
        }

        if (lastAccessed.HasValue && lastAccessed.Value < DateTime.Now.AddDays(-ageDays) && sizeBytes >= 100L * 1024 * 1024)
        {
            return ("Review old large file", 68, sizeBytes);
        }

        return ("Review", 40, 0);
    }

    public (DeletionCategory Category, string Explanation) ClassifyFolder(string path)
    {
        var normalized = Normalize(path);
        if (ProtectedFragments.Any(normalized.Contains))
        {
            return (DeletionCategory.DoNotDelete, "Protected folder. This app does not suggest deleting Windows, Program Files, driver, AppData, or app dependency folders.");
        }

        return (DeletionCategory.ReviewManually, "Folder sizes are informational. Open the folder and review contents before deleting anything.");
    }

    private static string Normalize(string path) => path.TrimEnd('\\').ToLowerInvariant() + "\\";

    private static bool IsCacheOrTempPath(string normalized)
    {
        return normalized.Contains(@"\temp\") ||
               normalized.Contains(@"\cache\") ||
               normalized.Contains(@"\caches\") ||
               normalized.Contains(@"\crashdumps\") ||
               normalized.Contains(@"\logs\") ||
               normalized.Contains(@"\nvidia\") && normalized.Contains(@"\installer");
    }

    private static bool IsOldInstaller(string extension, string path)
    {
        return extension.Equals(".msi", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".msp", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".exe", StringComparison.OrdinalIgnoreCase) && path.Contains("download", StringComparison.OrdinalIgnoreCase);
    }
}
