using Microsoft.Win32;
using SafeStorageScanner.Models;

namespace SafeStorageScanner.Services;

public sealed class InstalledAppScanner
{
    private static readonly string[] RegistryPaths =
    [
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
        @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
    ];

    public IReadOnlyList<InstalledAppInfo> Scan()
    {
        var apps = new List<InstalledAppInfo>();
        foreach (var hive in new[] { Registry.LocalMachine, Registry.CurrentUser })
        {
            foreach (var path in RegistryPaths)
            {
                using var key = hive.OpenSubKey(path);
                if (key is null)
                {
                    continue;
                }

                foreach (var subKeyName in key.GetSubKeyNames())
                {
                    using var appKey = key.OpenSubKey(subKeyName);
                    var displayName = appKey?.GetValue("DisplayName")?.ToString();
                    if (string.IsNullOrWhiteSpace(displayName))
                    {
                        continue;
                    }

                    apps.Add(new InstalledAppInfo
                    {
                        DisplayName = displayName,
                        Publisher = appKey?.GetValue("Publisher")?.ToString() ?? "",
                        InstallLocation = appKey?.GetValue("InstallLocation")?.ToString() ?? "",
                        InstallSizeBytes = ParseEstimatedSize(appKey?.GetValue("EstimatedSize")),
                        InstallDate = ParseDate(appKey?.GetValue("InstallDate")?.ToString()),
                        LastUsed = ParseDate(appKey?.GetValue("LastUsed")?.ToString())
                            ?? ParseDate(appKey?.GetValue("LastUseDate")?.ToString()),
                        Category = DeletionCategory.ReviewManually
                    });
                }
            }
        }

        return apps
            .GroupBy(a => new { a.DisplayName, a.InstallLocation })
            .Select(g => g.First())
            .OrderByDescending(a => a.InstallSizeBytes ?? 0)
            .ThenBy(a => a.DisplayName)
            .ToList();
    }

    private static long? ParseEstimatedSize(object? value)
    {
        if (value is null || !long.TryParse(value.ToString(), out var kilobytes))
        {
            return null;
        }

        return kilobytes * 1024;
    }

    private static DateTime? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateTime.TryParseExact(value, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var compact)
            ? compact
            : DateTime.TryParse(value, out var parsed) ? parsed : null;
    }
}
