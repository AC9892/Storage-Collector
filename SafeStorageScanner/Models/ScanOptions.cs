namespace SafeStorageScanner.Models;

public sealed class ScanOptions
{
    public string RootPath { get; init; } = "";
    public IReadOnlyList<string> RootPaths { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> ExcludedFolders { get; init; } = Array.Empty<string>();
    public long MinimumSizeBytes { get; init; }
    public bool FindDuplicates { get; init; }
    public bool VerifyDuplicateContent { get; init; }
    public bool LowStorageMode { get; init; }
    public int FileAgeDays { get; init; } = 365;
}
