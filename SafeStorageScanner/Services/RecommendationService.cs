using SafeStorageScanner.Models;

namespace SafeStorageScanner.Services;

public sealed class RecommendationService
{
    public IReadOnlyList<RecommendationItem> BuildQuickWins(IEnumerable<FileSystemScanItem> files, IEnumerable<DuplicateGroup> duplicates, IEnumerable<FileSystemScanItem> folders)
    {
        var recommendations = new List<RecommendationItem>();

        recommendations.AddRange(files
            .Where(f => f.EstimatedRecoverableBytes > 0)
            .OrderByDescending(f => f.EstimatedRecoverableBytes)
            .Take(200)
            .Select(f => new RecommendationItem
            {
                Kind = f.FileType,
                Path = f.Path,
                RecommendedAction = f.RecommendedAction,
                Category = f.CategoryText,
                EstimatedRecoverableBytes = f.EstimatedRecoverableBytes,
                ConfidenceScore = f.ConfidenceScore,
                Evidence = $"{f.SizeText}; last accessed {f.LastAccessed?.ToString("g") ?? "unknown"}; {f.Explanation}",
                RiskAssessment = f.Category == DeletionCategory.DoNotDelete ? "High risk" : f.ConfidenceScore >= 80 ? "Lower risk after review" : "Manual review required"
            }));

        recommendations.AddRange(duplicates.Take(100).Select(d => new RecommendationItem
        {
            Kind = "Duplicate",
            Path = d.Paths.Split(Environment.NewLine).FirstOrDefault() ?? "",
            RecommendedAction = d.RecommendedAction,
            Category = "Review manually",
            EstimatedRecoverableBytes = d.RecoverableBytes,
            ConfidenceScore = d.ConfidenceScore,
            Evidence = $"{d.Count} copies share size {d.SizeText} and hash {d.Hash}.",
            RiskAssessment = "Keep at least one copy. Check app/project ownership before deleting duplicates."
        }));

        recommendations.AddRange(folders
            .Where(f => LooksLikeRecycleBin(f.Path) && f.SizeBytes > 0)
            .Take(20)
            .Select(f => new RecommendationItem
            {
                Kind = "Recycle Bin",
                Path = f.Path,
                RecommendedAction = "Empty recycle bin through Windows",
                Category = "Review manually",
                EstimatedRecoverableBytes = f.SizeBytes,
                ConfidenceScore = 80,
                Evidence = $"Recycle Bin content under scanned drive uses {f.SizeText}.",
                RiskAssessment = "Files may still be restorable today. Emptying Recycle Bin removes that safety net."
            }));

        return recommendations
            .OrderByDescending(r => r.EstimatedRecoverableBytes)
            .ThenByDescending(r => r.ConfidenceScore)
            .ToList();
    }

    private static bool LooksLikeRecycleBin(string path) => path.Contains("$Recycle.Bin", StringComparison.OrdinalIgnoreCase);
}
