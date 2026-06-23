using System.IO;
using SafeStorageScanner.Models;

namespace SafeStorageScanner.Services;

public sealed class FileScanner
{
    private readonly SafetyClassifier _classifier;

    public FileScanner(SafetyClassifier classifier)
    {
        _classifier = classifier;
    }

    public async Task<ScanResult> ScanAsync(ScanOptions options, IProgress<string> progress, CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            var files = new List<FileSystemScanItem>();
            var folders = new Dictionary<string, FolderAccumulator>(StringComparer.OrdinalIgnoreCase);
            var root = new DirectoryInfo(options.RootPath);

            if (!root.Exists)
            {
                throw new DirectoryNotFoundException(options.RootPath);
            }

            Walk(root, options, files, folders, progress, cancellationToken);

            var folderItems = folders.Values
                .Where(f => f.Path.Length > 0)
                .Select(f =>
                {
                    var classified = _classifier.ClassifyFolder(f.Path);
                    var info = SafeDirectoryInfo(f.Path);
                    return new FileSystemScanItem
                    {
                        Path = f.Path,
                        Name = System.IO.Path.GetFileName(f.Path.TrimEnd('\\')),
                        ItemKind = "Folder",
                        FileType = "Folder",
                        SizeBytes = f.SizeBytes,
                        Modified = info?.LastWriteTime,
                        Created = info?.CreationTime,
                        LastAccessed = info?.LastAccessTime,
                        Category = classified.Category,
                        Explanation = classified.Explanation
                    };
                })
                .OrderByDescending(f => f.SizeBytes)
                .ToList();

            return new ScanResult(files.OrderByDescending(f => f.SizeBytes).ToList(), folderItems);
        }, cancellationToken);
    }

    private void Walk(DirectoryInfo directory, ScanOptions options, List<FileSystemScanItem> files, Dictionary<string, FolderAccumulator> folders, IProgress<string> progress, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (IsExcluded(directory.FullName, options.ExcludedFolders))
        {
            return;
        }

        progress.Report(directory.FullName);
        folders.TryAdd(directory.FullName, new FolderAccumulator(directory.FullName));

        FileInfo[] directoryFiles;
        try
        {
            directoryFiles = directory.GetFiles();
        }
        catch
        {
            return;
        }

        foreach (var file in directoryFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var length = SafeLength(file);
            AddSizeToParents(file.DirectoryName ?? directory.FullName, length, folders);
            if (length < options.MinimumSizeBytes)
            {
                continue;
            }

            var extension = file.Extension;
            var lastAccess = SafeLastAccess(file);
            var classified = _classifier.ClassifyFile(file.FullName, extension, length, lastAccess);
            var scored = _classifier.ScoreFile(file.FullName, extension, length, lastAccess, options.FileAgeDays);
            files.Add(new FileSystemScanItem
            {
                Path = file.FullName,
                Name = file.Name,
                ItemKind = "File",
                FileType = string.IsNullOrWhiteSpace(extension) ? "(none)" : extension,
                SizeBytes = length,
                Modified = SafeLastWrite(file),
                Created = SafeCreated(file),
                LastAccessed = lastAccess,
                Category = classified.Category,
                RecommendedAction = scored.Action,
                ConfidenceScore = scored.Confidence,
                EstimatedRecoverableBytes = classified.Category == DeletionCategory.DoNotDelete ? 0 : scored.RecoverableBytes,
                Explanation = classified.Explanation
            });
        }

        DirectoryInfo[] childDirectories;
        try
        {
            childDirectories = directory.GetDirectories();
        }
        catch
        {
            return;
        }

        foreach (var child in childDirectories)
        {
            Walk(child, options, files, folders, progress, cancellationToken);
        }
    }

    private static void AddSizeToParents(string directory, long size, Dictionary<string, FolderAccumulator> folders)
    {
        var current = directory;
        while (!string.IsNullOrWhiteSpace(current))
        {
            if (!folders.TryGetValue(current, out var accumulator))
            {
                accumulator = new FolderAccumulator(current);
                folders[current] = accumulator;
            }

            accumulator.SizeBytes += size;
            current = Directory.GetParent(current)?.FullName ?? "";
        }
    }

    private static bool IsExcluded(string path, IReadOnlyList<string> excludedFolders)
    {
        var normalizedPath = Normalize(path);
        return excludedFolders
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(Normalize)
            .Any(e => normalizedPath.Equals(e, StringComparison.OrdinalIgnoreCase) || normalizedPath.StartsWith(e + "\\", StringComparison.OrdinalIgnoreCase));
    }

    private static string Normalize(string path) => System.IO.Path.GetFullPath(path.Trim()).TrimEnd('\\');
    private static long SafeLength(FileInfo file) { try { return file.Length; } catch { return 0; } }
    private static DateTime? SafeLastWrite(FileInfo file) { try { return file.LastWriteTime; } catch { return null; } }
    private static DateTime? SafeCreated(FileInfo file) { try { return file.CreationTime; } catch { return null; } }
    private static DateTime? SafeLastAccess(FileInfo file) { try { return file.LastAccessTime; } catch { return null; } }
    private static DirectoryInfo? SafeDirectoryInfo(string path) { try { return new DirectoryInfo(path); } catch { return null; } }

    private sealed class FolderAccumulator
    {
        public FolderAccumulator(string path) => Path = path;
        public string Path { get; }
        public long SizeBytes { get; set; }
    }
}

public sealed record ScanResult(IReadOnlyList<FileSystemScanItem> Files, IReadOnlyList<FileSystemScanItem> Folders);
