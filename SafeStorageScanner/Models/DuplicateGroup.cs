namespace SafeStorageScanner.Models;

public sealed class DuplicateGroup
{
    public string Hash { get; init; } = "";
    public long SizeBytes { get; init; }
    public string SizeText => FileSystemScanItem.FormatBytes(SizeBytes);
    public int Count { get; init; }
    public long RecoverableBytes => Math.Max(0, Count - 1) * SizeBytes;
    public string RecoverableText => FileSystemScanItem.FormatBytes(RecoverableBytes);
    public string Paths { get; init; } = "";
    public string Explanation { get; init; } = "Files have the same size and SHA-256 hash. Keep at least one copy and review file locations before deleting anything.";
    public int ConfidenceScore { get; init; } = 95;
    public string RecommendedAction { get; init; } = "Review duplicate copies";
}
