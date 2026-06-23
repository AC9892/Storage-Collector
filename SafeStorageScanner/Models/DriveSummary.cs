namespace SafeStorageScanner.Models;

public sealed class DriveSummary
{
    public string Name { get; init; } = "";
    public string DriveType { get; init; } = "";
    public string FileSystem { get; init; } = "";
    public long TotalBytes { get; init; }
    public long FreeBytes { get; init; }
    public long UsedBytes => Math.Max(0, TotalBytes - FreeBytes);
    public double UsedPercent => TotalBytes <= 0 ? 0 : UsedBytes * 100.0 / TotalBytes;
    public string TotalText => FileSystemScanItem.FormatBytes(TotalBytes);
    public string UsedText => FileSystemScanItem.FormatBytes(UsedBytes);
    public string FreeText => FileSystemScanItem.FormatBytes(FreeBytes);
    public string TrendEstimate { get; init; } = "Trend requires multiple saved scans.";
    public string MoveTargetScore { get; init; } = "";
}
