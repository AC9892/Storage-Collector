using SafeStorageScanner.Models;

namespace SafeStorageScanner.Services;

public sealed class LocalAnalysisService
{
    public string Explain(FileSystemScanItem item)
    {
        var activity = item.LastAccessed.HasValue
            ? $"Windows says it was last accessed on {item.LastAccessed:g}."
            : "Windows did not provide a last-accessed date.";
        return $"{item.Name} is a {item.FileType} {item.ItemKind.ToLowerInvariant()} using {item.SizeText}. {activity} Recommendation: {item.RecommendedAction}. Confidence {item.ConfidenceScore}/100. Evidence: {item.Explanation} Risk: {(item.Category == DeletionCategory.DoNotDelete ? "Do not modify this item directly." : "Review the path, backups, and app ownership before taking action.")}";
    }

    public string ExplainApp(InstalledAppInfo app)
    {
        return $"{app.DisplayName} is installed by {Blank(app.Publisher)} at {Blank(app.InstallLocation)} and reports {app.InstallSizeText}. Windows last-used data: {app.LastUsedText}. Large apps and games should be uninstalled or moved using official app, launcher, or library tools when supported.";
    }

    public string Summarize(IEnumerable<RecommendationItem> recommendations)
    {
        var top = recommendations.OrderByDescending(r => r.EstimatedRecoverableBytes).FirstOrDefault();
        var total = recommendations.Sum(r => r.EstimatedRecoverableBytes);
        if (top is null)
        {
            return "No cleanup recommendation has enough evidence yet. Run a scan or lower the size threshold.";
        }

        return $"The current scan found about {FileSystemScanItem.FormatBytes(total)} of reviewable storage. The largest quick win is {top.Path}, which could recover {top.EstimatedRecoverableText}. Evidence: {top.Evidence}";
    }

    private static string Blank(string value) => string.IsNullOrWhiteSpace(value) ? "unknown" : value;
}
