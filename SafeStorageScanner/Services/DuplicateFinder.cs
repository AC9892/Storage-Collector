using System.Security.Cryptography;
using System.IO;
using SafeStorageScanner.Models;

namespace SafeStorageScanner.Services;

public sealed class DuplicateFinder
{
    public async Task<IReadOnlyList<DuplicateGroup>> FindAsync(IEnumerable<FileSystemScanItem> files, IProgress<string> progress, CancellationToken cancellationToken, bool verifyContent = false)
    {
        return await Task.Run(() =>
        {
            var candidates = files
                .Where(f => f.SizeBytes > 0 && f.Category != DeletionCategory.DoNotDelete)
                .GroupBy(f => f.SizeBytes)
                .Where(g => g.Count() > 1);

            var byHash = new Dictionary<string, List<FileSystemScanItem>>(StringComparer.OrdinalIgnoreCase);

            foreach (var sizeGroup in candidates)
            {
                foreach (var file in sizeGroup)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    progress.Report($"Hashing {file.Path}");
                    var hash = TryHash(file.Path);
                    if (hash is null)
                    {
                        continue;
                    }

                    if (!byHash.TryGetValue(hash, out var list))
                    {
                        list = [];
                        byHash[hash] = list;
                    }

                    list.Add(file);
                }
            }

            var groups = byHash
                .Where(kvp => kvp.Value.Count > 1)
                .Where(kvp => !verifyContent || VerifySameContent(kvp.Value))
                .Select(kvp => new DuplicateGroup
                {
                    Hash = kvp.Key,
                    SizeBytes = kvp.Value[0].SizeBytes,
                    Count = kvp.Value.Count,
                    Paths = string.Join(Environment.NewLine, kvp.Value.Select(f => f.Path))
                })
                .OrderByDescending(g => g.RecoverableBytes)
                .ToList();

            return groups;
        }, cancellationToken);
    }

    private static string? TryHash(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            var bytes = SHA256.HashData(stream);
            return Convert.ToHexString(bytes);
        }
        catch
        {
            return null;
        }
    }

    private static bool VerifySameContent(IReadOnlyList<FileSystemScanItem> files)
    {
        if (files.Count < 2)
        {
            return false;
        }

        try
        {
            var first = File.ReadAllBytes(files[0].Path);
            for (var i = 1; i < files.Count; i++)
            {
                var next = File.ReadAllBytes(files[i].Path);
                if (!first.AsSpan().SequenceEqual(next))
                {
                    return false;
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
