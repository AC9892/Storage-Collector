namespace SafeStorageScanner.Models;

public sealed class InstalledAppInfo
{
    public string DisplayName { get; init; } = "";
    public string Publisher { get; init; } = "";
    public string InstallLocation { get; init; } = "";
    public long? InstallSizeBytes { get; init; }
    public string InstallSizeText => InstallSizeBytes.HasValue
        ? FileSystemScanItem.FormatBytes(InstallSizeBytes.Value)
        : "Not provided";
    public DateTime? InstallDate { get; init; }
    public DateTime? LastUsed { get; init; }
    public string LastUsedText => LastUsed?.ToString("g") ?? "Not provided by Windows";
    public DeletionCategory Category { get; init; } = DeletionCategory.ReviewManually;
    public int ConfidenceScore { get; init; } = 55;
    public string RecommendedAction { get; init; } = "Uninstall or relocate if unused";
    public string CategoryText => Category switch
    {
        DeletionCategory.SafeToDelete => "Safe to delete",
        DeletionCategory.DoNotDelete => "Do not delete",
        _ => "Review manually"
    };
    public string Explanation { get; init; } = "Installed apps should be removed through Apps & Features, the app uninstaller, or supported launcher library tools. Do not delete app folders directly.";
}
