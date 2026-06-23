using System.IO;
using SafeStorageScanner.Models;

namespace SafeStorageScanner.Services;

public sealed class DriveService
{
    public IReadOnlyList<DriveSummary> GetDrives()
    {
        var drives = DriveInfo.GetDrives()
            .Where(d => d.IsReady)
            .Select(d => new DriveSummary
            {
                Name = d.RootDirectory.FullName,
                DriveType = d.DriveType.ToString(),
                FileSystem = d.DriveFormat,
                TotalBytes = d.TotalSize,
                FreeBytes = d.AvailableFreeSpace,
                MoveTargetScore = d.AvailableFreeSpace > 100L * 1024 * 1024 * 1024 ? "Good move target" : "Limited free space"
            })
            .OrderBy(d => d.Name)
            .ToList();

        var mostFree = drives.OrderByDescending(d => d.FreeBytes).FirstOrDefault();
        if (mostFree is null)
        {
            return drives;
        }

        return drives.Select(d => new DriveSummary
        {
            Name = d.Name,
            DriveType = d.DriveType,
            FileSystem = d.FileSystem,
            TotalBytes = d.TotalBytes,
            FreeBytes = d.FreeBytes,
            TrendEstimate = "Trend estimate will improve after multiple audit logs are available.",
            MoveTargetScore = d.Name == mostFree.Name ? "Best available target by free space" : d.MoveTargetScore
        }).ToList();
    }
}
