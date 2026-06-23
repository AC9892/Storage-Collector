namespace SafeStorageScanner.Services;

public sealed class FileTypeInfoService
{
    private readonly Dictionary<string, string> _descriptions = new(StringComparer.OrdinalIgnoreCase)
    {
        [".tmp"] = "Temporary file. Usually safe only when the related app is closed and the file is outside protected locations.",
        [".log"] = "Log file. Often safe to remove from non-system app folders, but keep logs needed for troubleshooting.",
        [".dmp"] = "Crash dump. Usually safe after troubleshooting, but it can help diagnose crashes.",
        [".zip"] = "Archive. Safe only if you no longer need the packaged contents.",
        [".iso"] = "Disk image. Often large and safe to remove after installation or backup, if you no longer need it.",
        [".mp4"] = "Video file. Personal media is usually safe to delete only when you confirm it is backed up or unwanted.",
        [".jpg"] = "Image file. Review manually; personal photos may be valuable.",
        [".png"] = "Image file. Review manually; it may be part of a project or app asset.",
        [".exe"] = "Program executable. Do not delete directly unless it is a known standalone installer in Downloads.",
        [".dll"] = "Program library. Do not delete directly; apps and Windows components can depend on it.",
        [".sys"] = "System driver. Do not delete directly.",
        [".msi"] = "Windows installer package. Review manually; some apps need installer caches for repair/uninstall."
    };

    public string Explain(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return "Folder or extensionless file. Review its location and parent app before deleting.";
        }

        return _descriptions.TryGetValue(extension, out var description)
            ? description
            : "Unknown file type. Use the path, publisher/app context, and backup status to decide. This app will not mark unknown files as automatically safe.";
    }
}
