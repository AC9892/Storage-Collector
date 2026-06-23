using System.IO;
using System.Text.Json;
using SafeStorageScanner.Models;

namespace SafeStorageScanner.Services;

public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public string SettingsPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SafeStorageScanner",
        "settings.json");

    public AppSettings Load()
    {
        if (!File.Exists(SettingsPath))
        {
            return CreateDefault();
        }

        try
        {
            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath), JsonOptions) ?? CreateDefault();
        }
        catch
        {
            return CreateDefault();
        }
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, JsonOptions));
    }

    private static AppSettings CreateDefault()
    {
        var systemDrive = Environment.GetEnvironmentVariable("SystemDrive") ?? "C:";
        var user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var settings = new AppSettings
        {
            TargetDrives = DriveInfo.GetDrives().Where(d => d.IsReady).Select(d => d.RootDirectory.FullName).Take(1).ToList(),
            Exclusions =
            [
                $@"{systemDrive}\Windows",
                $@"{systemDrive}\Program Files",
                $@"{systemDrive}\Program Files (x86)",
                $@"{user}\AppData",
                $@"{systemDrive}\Windows\System32"
            ]
        };
        settings.ScanLocations = CommonLocationService.GetCommonLocations(settings.TargetDrives).ToList();
        settings.Profiles.Add(new ScanProfile
        {
            Name = "Default cleanup review",
            TargetDrives = settings.TargetDrives.ToList(),
            ScanLocations = settings.ScanLocations.ToList(),
            RecommendationThresholdMb = settings.RecommendationThresholdMb,
            FileAgeDays = settings.FileAgeDays
        });
        return settings;
    }
}
