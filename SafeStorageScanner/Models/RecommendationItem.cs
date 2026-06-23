namespace SafeStorageScanner.Models;

public sealed class RecommendationItem
{
    public string Kind { get; init; } = "";
    public string Path { get; init; } = "";
    public string RecommendedAction { get; init; } = "";
    public string Category { get; init; } = "";
    public long EstimatedRecoverableBytes { get; init; }
    public string EstimatedRecoverableText => FileSystemScanItem.FormatBytes(EstimatedRecoverableBytes);
    public int ConfidenceScore { get; init; }
    public string Evidence { get; init; } = "";
    public string RiskAssessment { get; init; } = "";
}
