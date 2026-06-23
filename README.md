# Safe Storage Scanner

Safe Storage Scanner is a Windows desktop app built with C#/.NET 8 and WPF. It scans selected drives, folders, or common space-heavy locations; shows large files and folders; detects duplicate files; reads installed app metadata from the Windows registry; and records safety recommendations and user actions.

Dry-run mode is enabled by default. The app does not delete or uninstall anything. Move operations are only attempted when dry-run is turned off and the user confirms the move.

## Features

- Single Drive Scan dropdown for drives such as `C:\`, `D:\`, and `E:\`.
- Multi-Drive Scan using the selected drives from Settings.
- Low Storage Mode that targets selected drives and common heavy locations.
- Folder scan mode with a folder picker.
- Settings page for target drives, scan locations, thresholds, age rules, exclusions, scan profiles, schedule interval metadata, AI provider preference, beginner mode, advanced mode, dark mode, dry-run mode, and portable mode.
- Common location presets for Downloads, Desktop, Documents, Videos, Recycle Bin, temp folders, browser caches, launcher caches, update caches, logs, crash dumps, installer caches, Steam/Epic/Xbox game folders, and NVIDIA downloader caches where present.
- Quick Wins tab showing largest reviewable files, duplicates, temp/cache/log/installer-style files, and Recycle Bin contents first.
- Estimated recoverable storage per recommendation and for the full scan.
- Confidence score and evidence/risk explanation for every recommendation.
- Biggest files and biggest folders with path, size, type, modified date, created date, and last accessed date when Windows provides it.
- Installed apps and games view with install size, install location, publisher, install date, last-used data when Windows provides it, and guidance to uninstall or relocate through official tools.
- Conservative categories: `Safe to delete`, `Review manually`, and `Do not delete`.
- Safety explanations for each recommendation.
- Filters for size, file type, folder text, and last-access age.
- Customizable file age rules and size thresholds.
- Duplicate finder using file size grouping, SHA-256 hashing, and optional byte-by-byte content verification.
- "What is this file?" panel for common file types.
- AI Analysis tab with local rule-based plain-English explanations, risk assessments, scan summaries, and "Explain This" support for files, folders, apps, duplicates, and recommendations.
- Storage visualization view with a treemap and quick-win category chart.
- Drive dashboard with capacity, used/free space, filesystem, drive type, and best move-target hint.
- Move Instead of Delete planning that preserves folder structure under a chosen destination. Dry-run logs the move plan; confirmed moves require dry-run to be off.
- CSV, JSON, HTML, and simple PDF export.
- Complete audit logs for scans and move plans/actions.
- Portable mode that writes logs beside the app executable.
- Default excluded folders for Windows, Program Files, Program Files (x86), AppData, and System32.
- Right-click menus on result tables: copy path, open folder location, copy file size/info, command prompt here, and explain this.

## Project Structure

```text
SafeStorageScanner.slnx
README.md
SafeStorageScanner/
  App.xaml
  App.xaml.cs
  AssemblyInfo.cs
  MainWindow.xaml
  MainWindow.xaml.cs
  SafeStorageScanner.csproj
  Models/
    DeletionCategory.cs
    DriveSummary.cs
    DuplicateGroup.cs
    FileSystemScanItem.cs
    InstalledAppInfo.cs
    RecommendationItem.cs
    ScanOptions.cs
    AppSettings.cs
  Services/
    CommonLocationService.cs
    DriveService.cs
    DuplicateFinder.cs
    ExportService.cs
    FileScanner.cs
    FileTypeInfoService.cs
    InstalledAppScanner.cs
    LocalAnalysisService.cs
    RecommendationService.cs
    SafetyClassifier.cs
    SafetyLogService.cs
    SettingsService.cs
```

## Build

From this folder:

```powershell
dotnet restore .\SafeStorageScanner.slnx --ignore-failed-sources /p:NuGetAudit=false
dotnet build .\SafeStorageScanner.slnx -c Release
```

The app has no third-party NuGet packages. If the machine's user temp folder is low on space, redirect temp/cache to this workspace first:

```powershell
New-Item -ItemType Directory -Force .\.tmp\.nuget | Out-Null
$env:TEMP=(Resolve-Path .\.tmp).Path
$env:TMP=$env:TEMP
$env:NUGET_PACKAGES=(Resolve-Path .\.tmp\.nuget).Path
dotnet restore .\SafeStorageScanner.slnx --ignore-failed-sources /p:NuGetAudit=false
dotnet build .\SafeStorageScanner.slnx -c Release --no-restore
```

## Run

```powershell
dotnet run --project .\SafeStorageScanner\SafeStorageScanner.csproj
```

## Portable Publish

Framework-dependent portable folder:

```powershell
dotnet publish .\SafeStorageScanner\SafeStorageScanner.csproj -c Release -r win-x64 --self-contained false -o .\publish\SafeStorageScanner
```

Self-contained portable folder:

```powershell
dotnet publish .\SafeStorageScanner\SafeStorageScanner.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o .\publish\SafeStorageScannerPortable
```

In portable mode, scan logs are written under `SafetyLogs` beside the executable. With portable mode off, logs go under `%LOCALAPPDATA%\SafeStorageScanner\SafetyLogs`. Settings are stored at `%LOCALAPPDATA%\SafeStorageScanner\settings.json`.

## Admin Permissions

Normal user mode is recommended for everyday scans. It can scan folders the current user can access and avoids elevated writes.

Run as administrator only when you intentionally need visibility into protected folders. Even when elevated, the app still categorizes Windows, Program Files, driver, AppData, and dependency paths as `Do not delete`.

## Safety Notes

- Do not delete files from `Windows`, `System32`, `Program Files`, `Program Files (x86)`, driver folders, or app dependency folders based only on size.
- Remove installed apps through Windows Apps & Features or the app's uninstaller.
- Duplicate files are candidates for review, not automatic deletion. Keep at least one copy and verify app/project ownership.
- Last accessed dates can be unavailable or disabled depending on Windows settings and the file system.
- AI analysis is local rule-based explanation in this build. Cloud AI providers are represented as settings for future integration and are disabled by default.
- Scheduled scans are stored as profile/settings metadata in this build. No Windows Task Scheduler job is created yet.
- Restore/rollback support is logged for move operations where possible. Deletion and uninstall operations are not implemented.
