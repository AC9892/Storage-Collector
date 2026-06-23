using System.IO;

namespace SafeStorageScanner.Services;

public static class CommonLocationService
{
    public static IEnumerable<string> GetCommonLocations(IEnumerable<string> drives)
    {
        var user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        foreach (var drive in drives.Where(d => !string.IsNullOrWhiteSpace(d)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            yield return Path.Combine(drive, "$Recycle.Bin");
            yield return Path.Combine(drive, "Temp");
            yield return Path.Combine(drive, "Downloads");
            yield return Path.Combine(drive, "Games");
            yield return Path.Combine(drive, "SteamLibrary");
            yield return Path.Combine(drive, "Epic Games");
            yield return Path.Combine(drive, "XboxGames");
            yield return Path.Combine(drive, "ProgramData", "Package Cache");
            yield return Path.Combine(drive, "Windows", "SoftwareDistribution", "Download");
            yield return Path.Combine(drive, "Windows", "Temp");
        }

        var userLocations = new[]
        {
            Path.Combine(user, "Downloads"),
            Path.Combine(user, "Desktop"),
            Path.Combine(user, "Documents"),
            Path.Combine(user, "Videos"),
            Path.Combine(local, "Temp"),
            Path.Combine(local, "CrashDumps"),
            Path.Combine(local, "Microsoft", "Windows", "INetCache"),
            Path.Combine(local, "Google", "Chrome", "User Data", "Default", "Cache"),
            Path.Combine(local, "Microsoft", "Edge", "User Data", "Default", "Cache"),
            Path.Combine(roaming, "Mozilla", "Firefox", "Profiles"),
            Path.Combine(local, "NVIDIA Corporation", "Downloader"),
            Path.Combine(local, "D3DSCache"),
            Path.Combine(local, "Steam", "htmlcache"),
            Path.Combine(local, "EpicGamesLauncher", "Saved", "webcache")
        };

        foreach (var path in userLocations)
        {
            yield return path;
        }
    }
}
