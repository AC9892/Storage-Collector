namespace SafeStorageScanner.Models;

public sealed class FileSystemScanItem
{
    public string Path { get; init; } = "";
    public string Name { get; init; } = "";
    public string ItemKind { get; init; } = "";
    public string FileType { get; init; } = "";
    public long SizeBytes { get; init; }
    public string SizeText => FormatBytes(SizeBytes);
    public DateTime? Modified { get; init; }
    public DateTime? Created { get; init; }
    public DateTime? LastAccessed { get; init; }
    public DeletionCategory Category { get; init; }
    public string RecommendedAction { get; init; } = "Review";
    public int ConfidenceScore { get; init; }
    public long EstimatedRecoverableBytes { get; init; }
    public string EstimatedRecoverableText => FormatBytes(EstimatedRecoverableBytes);
    public string CategoryText => Category switch
    {
        DeletionCategory.SafeToDelete => "Safe to delete",
        DeletionCategory.DoNotDelete => "Do not delete",
        _ => "Review manually"
    };
    public string Explanation { get; init; } = "";

    public static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB", "PB"];
        double value = bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{value:0.##} {units[unit]}";
    }
}
