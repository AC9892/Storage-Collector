using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using SafeStorageScanner.Models;
using SafeStorageScanner.Services;
using WinForms = System.Windows.Forms;

namespace SafeStorageScanner;

public partial class MainWindow : Window
{
    private readonly SafetyClassifier _classifier = new();
    private readonly FileTypeInfoService _fileTypeInfo = new();
    private readonly InstalledAppScanner _appScanner = new();
    private readonly DuplicateFinder _duplicateFinder = new();
    private readonly ExportService _exportService = new();
    private readonly SafetyLogService _logService = new();
    private readonly SettingsService _settingsService = new();
    private readonly DriveService _driveService = new();
    private readonly RecommendationService _recommendationService = new();
    private readonly LocalAnalysisService _analysisService = new();

    private readonly ObservableCollection<FileSystemScanItem> _shownFiles = [];
    private readonly ObservableCollection<FileSystemScanItem> _shownFolders = [];
    private readonly ObservableCollection<DuplicateGroup> _shownDuplicates = [];
    private readonly ObservableCollection<InstalledAppInfo> _apps = [];
    private readonly ObservableCollection<RecommendationItem> _quickWins = [];
    private readonly ObservableCollection<DriveSummary> _drives = [];

    private AppSettings _settings = new();
    private List<FileSystemScanItem> _allFiles = [];
    private List<FileSystemScanItem> _allFolders = [];
    private List<DuplicateGroup> _allDuplicates = [];
    private CancellationTokenSource? _scanCancellation;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            EnsureInitialPageSelected();
            ThemeManager.ApplyToWindow(this);
            Dispatcher.BeginInvoke(UpdateNavigationHighlight, System.Windows.Threading.DispatcherPriority.ContextIdle);
        };
        FilesGrid.ItemsSource = _shownFiles;
        FoldersGrid.ItemsSource = _shownFolders;
        DuplicatesGrid.ItemsSource = _shownDuplicates;
        AppsGrid.ItemsSource = _apps;
        QuickWinsGrid.ItemsSource = _quickWins;
        DrivesGrid.ItemsSource = _drives;
        DashboardFoldersGrid.ItemsSource = _shownFolders;
        InstallContextMenus();

        LoadDrives();
        LoadSettingsIntoUi();
        RefreshDriveDashboard();
        EnsureInitialPageSelected();

        FolderRadio.Checked += (_, _) => UpdateScanTargetUi();
        SingleDriveRadio.Checked += (_, _) => UpdateScanTargetUi();
        MultiDriveRadio.Checked += (_, _) => UpdateScanTargetUi();
        UpdateScanTargetUi();
    }

    private void EnsureInitialPageSelected()
    {
        if (NavigationList.SelectedIndex < 0)
        {
            NavigationList.SelectedIndex = 0;
        }

        if (MainTabs.SelectedIndex != NavigationList.SelectedIndex)
        {
            MainTabs.SelectedIndex = NavigationList.SelectedIndex;
        }

        UpdateNavigationHighlight();
    }

    private void InstallContextMenus()
    {
        foreach (var grid in new[] { QuickWinsGrid, FilesGrid, FoldersGrid, DuplicatesGrid, AppsGrid })
        {
            grid.ContextMenu = BuildGridContextMenu();
        }
    }

    private ContextMenu BuildGridContextMenu()
    {
        var menu = new ContextMenu();
        menu.Items.Add(BuildMenuItem("Copy path", CopyPath_Click));
        menu.Items.Add(BuildMenuItem("Open folder location", OpenFolderLocation_Click));
        menu.Items.Add(BuildMenuItem("Copy file size and info", CopyInfo_Click));
        menu.Items.Add(BuildMenuItem("Command prompt here", CommandPromptHere_Click));
        menu.Items.Add(new Separator());
        menu.Items.Add(BuildMenuItem("Explain this", ExplainThis_Click));
        return menu;
    }

    private static MenuItem BuildMenuItem(string header, RoutedEventHandler handler)
    {
        var item = new MenuItem { Header = header };
        item.Click += handler;
        return item;
    }

    private void NavigationList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MainTabs is null || NavigationList.SelectedIndex < 0)
        {
            return;
        }

        MainTabs.SelectedIndex = NavigationList.SelectedIndex;
        UpdateNavigationHighlight();
        (PageTitleTextBlock.Text, PageSubtitleTextBlock.Text) = NavigationList.SelectedIndex switch
        {
            0 => ("Dashboard", "Review drive health, quick wins, and recent scan context."),
            1 => ("Scan", "Choose a target and start a focused storage scan."),
            2 => ("Results", "Review quick wins, largest files, and largest folders."),
            3 => ("Duplicates", "Compare duplicate files by size, hash, and optional content verification."),
            4 => ("Installed Apps", "Find large apps and games, then uninstall or relocate through supported tools."),
            5 => ("Storage Visualization", "See folder usage with treemaps and storage breakdown charts."),
            6 => ("AI Analysis", "Plain-English explanations and cleanup suggestions."),
            7 => ("Reports", "Export reports and review audit-log locations."),
            _ => ("Dashboard", "")
        };
    }

    private void UpdateNavigationHighlight()
    {
        var selectedBrush = (System.Windows.Media.Brush)FindResource("SelectionBrush");
        var textBrush = (System.Windows.Media.Brush)FindResource("TextBrush");
        for (var i = 0; i < NavigationList.Items.Count; i++)
        {
            if (NavigationList.Items[i] is not ListBoxItem item)
            {
                continue;
            }

            if (i == NavigationList.SelectedIndex)
            {
                item.Background = selectedBrush;
                item.Foreground = System.Windows.Media.Brushes.White;
                item.BorderBrush = selectedBrush;
            }
            else
            {
                item.Background = System.Windows.Media.Brushes.Transparent;
                item.Foreground = textBrush;
                item.BorderBrush = System.Windows.Media.Brushes.Transparent;
            }
        }
    }

    private void OpenSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var window = new SettingsWindow(_settingsService)
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            ThemeManager.ApplyToWindow(window);
            if (window.ShowDialog() == true)
            {
                LoadSettingsIntoUi();
                ThemeManager.ApplyTheme(ResolveTheme());
                StatusTextBlock.Text = "Settings updated.";
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Settings could not be opened", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusTextBlock.Text = "Settings failed to open.";
        }
    }

    private void LoadDrives()
    {
        var drives = DriveInfo.GetDrives().Where(d => d.IsReady).Select(d => d.RootDirectory.FullName).ToList();
        DriveComboBox.ItemsSource = drives;
        DriveComboBox.SelectedIndex = drives.Count > 0 ? 0 : -1;
        MultiDriveChoicesPanel.Children.Clear();
        foreach (var drive in drives)
        {
            var button = new ToggleButton
            {
                Content = drive,
                Tag = "DriveToggle",
                IsChecked = _settings.TargetDrives.Count == 0 || _settings.TargetDrives.Contains(drive, StringComparer.OrdinalIgnoreCase),
                Margin = new Thickness(0, 0, 10, 8),
                MinWidth = 84,
                MinHeight = 38,
                Padding = new Thickness(12, 8, 12, 8),
                BorderThickness = new Thickness(1),
                FocusVisualStyle = null,
                Template = DriveToggleTemplate()
            };
            button.Checked += (_, _) => StyleDriveToggle(button);
            button.Unchecked += (_, _) => StyleDriveToggle(button);
            StyleDriveToggle(button);
            MultiDriveChoicesPanel.Children.Add(button);
        }
    }

    private void LoadSettingsIntoUi()
    {
        _settings = _settingsService.Load();
        LowStorageModeCheckBox.IsChecked = _settings.LowStorageMode;
        DryRunCheckBox.IsChecked = _settings.DryRunMode;
        PortableModeCheckBox.IsChecked = _settings.PortableMode;
        VerifyDuplicateCheckBox.IsChecked = _settings.VerifyDuplicateContent;
        MinSizeTextBox.Text = _settings.RecommendationThresholdMb.ToString();
        if (!string.IsNullOrWhiteSpace(_settings.SingleDriveDefault) && DriveComboBox.Items.Contains(_settings.SingleDriveDefault))
        {
            DriveComboBox.SelectedItem = _settings.SingleDriveDefault;
        }
        if (!string.IsNullOrWhiteSpace(_settings.FolderDefault))
        {
            FolderTextBox.Text = _settings.FolderDefault;
        }
        foreach (var button in MultiDriveChoicesPanel.Children.OfType<ToggleButton>())
        {
            var drive = button.Content?.ToString() ?? "";
            button.IsChecked = _settings.TargetDrives.Count == 0 || _settings.TargetDrives.Contains(drive, StringComparer.OrdinalIgnoreCase);
            StyleDriveToggle(button);
        }
        ApplyTheme();
    }

    private void StyleDriveToggle(ToggleButton button)
    {
        var selected = button.IsChecked == true;
        button.Background = FindBrush(selected ? "SelectionBrush" : "InputBackground");
        button.Foreground = selected ? System.Windows.Media.Brushes.White : FindBrush("TextBrush");
        button.BorderBrush = FindBrush(selected ? "SelectionBrush" : "BorderBrushSoft");
    }

    private System.Windows.Media.Brush FindBrush(string key) => TryFindResource(key) as System.Windows.Media.Brush ?? System.Windows.Media.Brushes.Transparent;

    private void UpdateDriveToggleStyles()
    {
        foreach (var button in MultiDriveChoicesPanel.Children.OfType<ToggleButton>())
        {
            StyleDriveToggle(button);
        }
    }

    private static ControlTemplate DriveToggleTemplate()
    {
        var template = new ControlTemplate(typeof(ToggleButton));
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
        border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(System.Windows.Controls.Control.BackgroundProperty));
        border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(System.Windows.Controls.Control.BorderBrushProperty));
        border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(System.Windows.Controls.Control.BorderThicknessProperty));
        border.SetValue(Border.SnapsToDevicePixelsProperty, true);

        var content = new FrameworkElementFactory(typeof(ContentPresenter));
        content.SetValue(ContentPresenter.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Center);
        content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        content.SetValue(FrameworkElement.MarginProperty, new TemplateBindingExtension(System.Windows.Controls.Control.PaddingProperty));
        content.SetValue(System.Windows.Documents.TextElement.ForegroundProperty, new TemplateBindingExtension(System.Windows.Controls.Control.ForegroundProperty));
        border.AppendChild(content);

        template.VisualTree = border;
        return template;
    }

    private void UpdateScanTargetUi()
    {
        var folderMode = FolderRadio.IsChecked == true;
        var multiMode = MultiDriveRadio.IsChecked == true;
        DriveComboBox.IsEnabled = !folderMode && !multiMode;
        FolderTextBox.IsEnabled = folderMode;
        MultiDrivePanel.Visibility = multiMode ? Visibility.Visible : Visibility.Collapsed;
    }

    private void BrowseFolder_Click(object sender, RoutedEventArgs e)
    {
        var folder = BrowseForFolder("Choose a folder to scan");
        if (folder is null) return;
        FolderRadio.IsChecked = true;
        FolderTextBox.Text = folder;
    }

    private void BrowseMoveTarget_Click(object sender, RoutedEventArgs e)
    {
        var folder = BrowseForFolder("Choose a target folder or drive for moved files");
        if (folder is not null) MoveTargetTextBox.Text = folder;
    }

    private static string? BrowseForFolder(string description)
    {
        using var dialog = new WinForms.FolderBrowserDialog { Description = description, UseDescriptionForTitle = true };
        return dialog.ShowDialog() == WinForms.DialogResult.OK ? dialog.SelectedPath : null;
    }

    private async void StartScan_Click(object sender, RoutedEventArgs e)
    {
        var roots = ResolveScanRoots().Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (roots.Count == 0)
        {
            System.Windows.MessageBox.Show("Choose at least one existing drive, folder, or scan location first.", "Scan target needed", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        SetScanningState(true);
        _scanCancellation = new CancellationTokenSource();
        _allFiles = [];
        _allFolders = [];
        _allDuplicates = [];

        try
        {
            foreach (var root in roots)
            {
                var options = BuildScanOptions(root);
                var progress = new Progress<string>(message => StatusTextBlock.Text = $"Scanning {message}");
                var result = await new FileScanner(_classifier).ScanAsync(options, progress, _scanCancellation.Token);
                _allFiles.AddRange(result.Files);
                _allFolders.AddRange(result.Folders);
            }

            _allFiles = _allFiles.OrderByDescending(f => f.SizeBytes).ToList();
            _allFolders = _allFolders.GroupBy(f => f.Path, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.OrderByDescending(f => f.SizeBytes).First())
                .OrderByDescending(f => f.SizeBytes)
                .ToList();
            ApplyFilters();

            if (DuplicateCheckBox.IsChecked == true)
            {
                StatusTextBlock.Text = "Finding duplicates by size and hash...";
                var duplicateProgress = new Progress<string>(message => StatusTextBlock.Text = message);
                _allDuplicates = (await _duplicateFinder.FindAsync(_allFiles, duplicateProgress, _scanCancellation.Token, VerifyDuplicateCheckBox.IsChecked == true)).ToList();
            }

            RefreshDuplicates();
            RefreshQuickWins();
            DrawTreemap();
            DrawCharts();
            var logPath = _logService.RecordScan(string.Join(";", roots), _allFiles, _allFolders, _allDuplicates);
            LogPathTextBlock.Text = $"Safety log: {logPath}";
            UpdateDashboardStats();
            AiSummaryTextBlock.Text = _analysisService.Summarize(_quickWins);
            StatusTextBlock.Text = "Scan complete. Dry-run remains available; no action runs automatically.";
        }
        catch (OperationCanceledException)
        {
            StatusTextBlock.Text = "Scan canceled.";
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = "Scan failed.";
            System.Windows.MessageBox.Show(ex.Message, "Scan failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetScanningState(false);
        }
    }

    private ScanOptions BuildScanOptions(string root)
    {
        var ageDays = _settings.FileAgeDays;
        return new ScanOptions
        {
            RootPath = root,
            RootPaths = ResolveScanRoots().ToList(),
            ExcludedFolders = _settings.Exclusions,
            MinimumSizeBytes = ParseMinimumBytes(),
            FindDuplicates = DuplicateCheckBox.IsChecked == true,
            VerifyDuplicateContent = VerifyDuplicateCheckBox.IsChecked == true,
            LowStorageMode = LowStorageModeCheckBox.IsChecked == true,
            FileAgeDays = ageDays
        };
    }

    private IEnumerable<string> ResolveScanRoots()
    {
        if (FolderRadio.IsChecked == true)
        {
            yield return FolderTextBox.Text;
            yield break;
        }

        if (MultiDriveRadio.IsChecked == true || LowStorageModeCheckBox.IsChecked == true)
        {
            var selectedDrives = MultiDriveChoicesPanel.Children
                .OfType<ToggleButton>()
                .Where(c => c.IsChecked == true)
                .Select(c => c.Content?.ToString())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Cast<string>()
                .ToList();

            if (selectedDrives.Count == 0)
            {
                selectedDrives = _settings.TargetDrives.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            }

            foreach (var selected in selectedDrives)
            {
                yield return selected;
            }

            if (LowStorageModeCheckBox.IsChecked == true)
            {
                foreach (var location in _settings.ScanLocations)
                {
                    yield return location;
                }
            }
            yield break;
        }

        if (DriveComboBox.SelectedItem?.ToString() is { Length: > 0 } drive)
        {
            yield return drive;
        }
    }

    private void SetScanningState(bool scanning)
    {
        ScanButton.IsEnabled = !scanning;
        CancelButton.IsEnabled = scanning;
        _logService.Configure(PortableModeCheckBox.IsChecked == true);
    }

    private long ParseMinimumBytes() => ParseLong(MinSizeTextBox.Text, 0) * 1024 * 1024;
    private static int ParseInt(string text, int fallback) => int.TryParse(text, out var value) ? value : fallback;
    private static long ParseLong(string text, long fallback) => long.TryParse(text, out var value) ? Math.Max(0, value) : fallback;

    private void Cancel_Click(object sender, RoutedEventArgs e) => _scanCancellation?.Cancel();

    private void LoadInstalledApps_Click(object sender, RoutedEventArgs e)
    {
        _apps.Clear();
        foreach (var app in _appScanner.Scan())
        {
            _apps.Add(app);
        }
        StatusTextBlock.Text = $"Loaded {_apps.Count:N0} installed apps. Use official uninstallers or launcher library tools.";
    }

    private void ApplyFilters_Click(object sender, RoutedEventArgs e) => ApplyFilters();

    private void ClearFilters_Click(object sender, RoutedEventArgs e)
    {
        TypeFilterTextBox.Text = "";
        FolderFilterTextBox.Text = "";
        AgeFilterComboBox.SelectedIndex = 0;
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        var typeFilter = TypeFilterTextBox.Text.Trim();
        var folderFilter = FolderFilterTextBox.Text.Trim();
        var cutoff = GetDateCutoff();

        var filteredFiles = _allFiles.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(typeFilter))
        {
            filteredFiles = filteredFiles.Where(f => f.FileType.Contains(typeFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(folderFilter))
        {
            filteredFiles = filteredFiles.Where(f => f.Path.Contains(folderFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (cutoff.HasValue)
        {
            filteredFiles = filteredFiles.Where(f => f.LastAccessed.HasValue && f.LastAccessed.Value < cutoff.Value);
        }

        RefreshCollection(_shownFiles, filteredFiles.Take(10000));
        RefreshCollection(_shownFolders, _allFolders.Take(5000));
        RefreshQuickWins();
        DrawTreemap();
        DrawCharts();
        UpdateDashboardStats();
    }

    private DateTime? GetDateCutoff()
    {
        var text = (AgeFilterComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
        return text switch
        {
            "30 days" => DateTime.Now.AddDays(-30),
            "90 days" => DateTime.Now.AddDays(-90),
            "180 days" => DateTime.Now.AddDays(-180),
            "365 days" => DateTime.Now.AddDays(-365),
            "2 years" => DateTime.Now.AddYears(-2),
            _ => null
        };
    }

    private void RefreshDuplicates() => RefreshCollection(_shownDuplicates, _allDuplicates.Take(2000));

    private void RefreshQuickWins()
    {
        RefreshCollection(_quickWins, _recommendationService.BuildQuickWins(_shownFiles, _allDuplicates, _shownFolders).Take(1000));
    }

    private void RefreshDriveDashboard()
    {
        RefreshCollection(_drives, _driveService.GetDrives());
    }

    private void UpdateDashboardStats()
    {
        RecoverableTextBlock.Text = FileSystemScanItem.FormatBytes(_quickWins.Sum(q => q.EstimatedRecoverableBytes));
        FilesScannedTextBlock.Text = _allFiles.Count.ToString("N0");
        DuplicateGroupsTextBlock.Text = _allDuplicates.Count.ToString("N0");
        InstalledAppsCountTextBlock.Text = _apps.Count.ToString("N0");
        SummaryTextBlock.Text = $"{_allFiles.Count:N0} files, {_allFolders.Count:N0} folders, {_allDuplicates.Count:N0} duplicate groups";
    }

    private static void RefreshCollection<T>(ObservableCollection<T> collection, IEnumerable<T> values)
    {
        collection.Clear();
        foreach (var value in values)
        {
            collection.Add(value);
        }
    }

    private void ExportFilesCsv_Click(object sender, RoutedEventArgs e)
    {
        var path = ChooseSavePath("CSV file (*.csv)|*.csv", "storage-scan.csv");
        if (path is null) return;
        _exportService.ExportFilesCsv(path, _shownFiles.Concat(_shownFolders));
        StatusTextBlock.Text = $"Exported CSV: {path}";
    }

    private void ExportFilesJson_Click(object sender, RoutedEventArgs e)
    {
        var path = ChooseSavePath("JSON file (*.json)|*.json", "storage-scan.json");
        if (path is null) return;
        _exportService.ExportFilesJson(path, _shownFiles.Concat(_shownFolders));
        StatusTextBlock.Text = $"Exported JSON: {path}";
    }

    private void ExportDuplicatesJson_Click(object sender, RoutedEventArgs e)
    {
        var path = ChooseSavePath("JSON file (*.json)|*.json", "duplicates.json");
        if (path is null) return;
        _exportService.ExportDuplicatesJson(path, _shownDuplicates);
        StatusTextBlock.Text = $"Exported duplicates JSON: {path}";
    }

    private void ExportHtml_Click(object sender, RoutedEventArgs e)
    {
        var path = ChooseSavePath("HTML report (*.html)|*.html", "storage-report.html");
        if (path is null) return;
        _exportService.ExportHtml(path, _quickWins);
        StatusTextBlock.Text = $"Exported HTML report: {path}";
    }

    private void ExportPdf_Click(object sender, RoutedEventArgs e)
    {
        var path = ChooseSavePath("PDF file (*.pdf)|*.pdf", "storage-report.pdf");
        if (path is null) return;
        _exportService.ExportPdf(path, _quickWins);
        StatusTextBlock.Text = $"Exported PDF report: {path}";
    }

    private static string? ChooseSavePath(string filter, string fileName)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog { Filter = filter, FileName = fileName };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    private void ApplyTheme()
    {
        ThemeManager.ApplyTheme(ResolveTheme());
        Dispatcher.BeginInvoke(UpdateNavigationHighlight, System.Windows.Threading.DispatcherPriority.ContextIdle);
        Dispatcher.BeginInvoke(UpdateDriveToggleStyles, System.Windows.Threading.DispatcherPriority.ContextIdle);
    }

    private AppTheme ResolveTheme()
    {
        if (_settings.Theme.Equals("Dark", StringComparison.OrdinalIgnoreCase) || _settings.DarkMode)
        {
            return AppTheme.Dark;
        }

        return AppTheme.Light;
    }

    private void FilesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FilesGrid.SelectedItem is FileSystemScanItem item) ShowFileTypeInfo(item.FileType);
    }

    private void FoldersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FoldersGrid.SelectedItem is FileSystemScanItem item) ShowFileTypeInfo(item.FileType);
    }

    private void QuickWinsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (QuickWinsGrid.SelectedItem is RecommendationItem item) AiOutputTextBox.Text = $"{item.RecommendedAction}\n\nEvidence: {item.Evidence}\n\nRisk: {item.RiskAssessment}";
    }

    private void ShowFileTypeInfo(string extension) => StatusTextBlock.Text = _fileTypeInfo.Explain(extension);

    private object? CurrentSelectedItem()
    {
        return MainTabs.SelectedContent switch
        {
            DataGrid grid => grid.SelectedItem,
            _ => FilesGrid.SelectedItem ?? FoldersGrid.SelectedItem ?? QuickWinsGrid.SelectedItem ?? AppsGrid.SelectedItem
        };
    }

    private string? CurrentSelectedPath()
    {
        return CurrentSelectedItem() switch
        {
            FileSystemScanItem item => item.Path,
            RecommendationItem item => item.Path,
            InstalledAppInfo item => item.InstallLocation,
            DuplicateGroup item => item.Paths.Split(Environment.NewLine).FirstOrDefault(),
            _ => null
        };
    }

    private void CopyPath_Click(object sender, RoutedEventArgs e)
    {
        var path = CurrentSelectedPath();
        if (!string.IsNullOrWhiteSpace(path)) System.Windows.Clipboard.SetText(path);
    }

    private void OpenFolderLocation_Click(object sender, RoutedEventArgs e)
    {
        var path = CurrentSelectedPath();
        if (string.IsNullOrWhiteSpace(path)) return;
        var folder = Directory.Exists(path) ? path : Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(folder) && Directory.Exists(folder))
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{folder}\"") { UseShellExecute = true });
        }
    }

    private void CopyInfo_Click(object sender, RoutedEventArgs e)
    {
        var text = CurrentSelectedItem() switch
        {
            FileSystemScanItem item => $"{item.Path}\nSize: {item.SizeText}\nType: {item.FileType}\nCategory: {item.CategoryText}\nConfidence: {item.ConfidenceScore}\nWhy: {item.Explanation}",
            RecommendationItem item => $"{item.Path}\nRecoverable: {item.EstimatedRecoverableText}\nConfidence: {item.ConfidenceScore}\nAction: {item.RecommendedAction}\nEvidence: {item.Evidence}",
            DuplicateGroup item => $"Duplicate group\nEach size: {item.SizeText}\nRecoverable: {item.RecoverableText}\nHash: {item.Hash}\nPaths:\n{item.Paths}",
            InstalledAppInfo item => $"{item.DisplayName}\nSize: {item.InstallSizeText}\nLocation: {item.InstallLocation}\nPublisher: {item.Publisher}\nLast used: {item.LastUsedText}",
            _ => ""
        };
        if (!string.IsNullOrWhiteSpace(text)) System.Windows.Clipboard.SetText(text);
    }

    private void CommandPromptHere_Click(object sender, RoutedEventArgs e)
    {
        var path = CurrentSelectedPath();
        var folder = Directory.Exists(path) ? path : Path.GetDirectoryName(path ?? "");
        if (!string.IsNullOrWhiteSpace(folder) && Directory.Exists(folder))
        {
            Process.Start(new ProcessStartInfo("cmd.exe") { WorkingDirectory = folder, UseShellExecute = true });
        }
    }

    private void ExplainThis_Click(object sender, RoutedEventArgs e)
    {
        AiOutputTextBox.Text = CurrentSelectedItem() switch
        {
            FileSystemScanItem item => _analysisService.Explain(item),
            RecommendationItem item => $"{item.RecommendedAction}: {item.Path}\n\nEvidence: {item.Evidence}\n\nRisk: {item.RiskAssessment}",
            InstalledAppInfo app => _analysisService.ExplainApp(app),
            DuplicateGroup d => $"These {d.Count} files have the same size and SHA-256 hash. Possible recovery is {d.RecoverableText}. Keep one copy and review every path before action.\n\n{d.Paths}",
            _ => "Select a file, folder, app, duplicate, or recommendation first."
        };
        MainTabs.SelectedIndex = 6;
    }

    private void SummarizeScan_Click(object sender, RoutedEventArgs e) => AiOutputTextBox.Text = _analysisService.Summarize(_quickWins);

    private void PlanMove_Click(object sender, RoutedEventArgs e)
    {
        var path = CurrentSelectedPath();
        var targetRoot = MoveTargetTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(targetRoot) || !Directory.Exists(targetRoot))
        {
            System.Windows.MessageBox.Show("Select an item and choose an existing move target first.", "Move target needed", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var driveRoot = Path.GetPathRoot(path) ?? "";
        var relative = path.StartsWith(driveRoot, StringComparison.OrdinalIgnoreCase) ? path[driveRoot.Length..] : Path.GetFileName(path);
        var destination = Path.Combine(targetRoot, relative);

        if (DryRunCheckBox.IsChecked == true)
        {
            _logService.RecordAction("Dry-run move plan", $"{path} -> {destination}", EstimateSelectedBytes());
            StatusTextBlock.Text = $"Dry-run move plan: {path} -> {destination}";
            return;
        }

        var confirm = System.Windows.MessageBox.Show($"Move this item?\n\nFrom: {path}\nTo: {destination}\n\nFolder structure will be preserved where possible.", "Confirm move", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;

        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        if (File.Exists(path))
        {
            File.Move(path, destination, overwrite: false);
        }
        else if (Directory.Exists(path))
        {
            Directory.Move(path, destination);
        }
        _logService.RecordAction("Confirmed move", $"{path} -> {destination}", EstimateSelectedBytes());
        StatusTextBlock.Text = $"Moved to {destination}.";
    }

    private long EstimateSelectedBytes()
    {
        return CurrentSelectedItem() switch
        {
            FileSystemScanItem item => item.SizeBytes,
            RecommendationItem item => item.EstimatedRecoverableBytes,
            DuplicateGroup item => item.RecoverableBytes,
            _ => 0
        };
    }

    private void TreemapCanvas_SizeChanged(object sender, SizeChangedEventArgs e) => DrawTreemap();

    private void DrawTreemap()
    {
        if (!IsLoaded || TreemapCanvas.ActualWidth <= 1 || TreemapCanvas.ActualHeight <= 1) return;
        TreemapCanvas.Children.Clear();
        var folders = _allFolders.Where(f => f.SizeBytes > 0).Take(30).ToList();
        var total = folders.Sum(f => f.SizeBytes);
        if (total <= 0) return;

        var x = 0.0;
        var y = 0.0;
        var width = TreemapCanvas.ActualWidth;
        var height = TreemapCanvas.ActualHeight;
        var palette = new[] { "#2563EB", "#059669", "#D97706", "#7C3AED", "#DC2626", "#0891B2", "#4B5563", "#BE123C" };

        for (var i = 0; i < folders.Count && height > 40; i++)
        {
            var item = folders[i];
            var rectWidth = Math.Min(width, Math.Max(60, TreemapCanvas.ActualWidth * ((double)item.SizeBytes / total)));
            var rectHeight = Math.Min(height, Math.Max(42, height / Math.Max(1, folders.Count - i)));
            var border = new Border
            {
                Width = rectWidth,
                Height = rectHeight,
                Background = (System.Windows.Media.Brush)new BrushConverter().ConvertFromString(palette[i % palette.Length])!,
                BorderBrush = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(2),
                ToolTip = $"{item.Path}\n{item.SizeText}"
            };
            border.Child = new TextBlock
            {
                Text = $"{Path.GetFileName(item.Path.TrimEnd('\\'))}\n{item.SizeText}",
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(6)
            };
            border.MouseLeftButtonUp += (_, _) => StatusTextBlock.Text = $"{item.Path} uses {item.SizeText}";
            Canvas.SetLeft(border, x);
            Canvas.SetTop(border, y);
            TreemapCanvas.Children.Add(border);
            y += rectHeight;
            height -= rectHeight;
        }
    }

    private void DrawCharts()
    {
        var raw = _quickWins.GroupBy(q => q.RecommendedAction)
            .Select(g => new { Action = g.Key, Bytes = g.Sum(x => x.EstimatedRecoverableBytes), Text = FileSystemScanItem.FormatBytes(g.Sum(x => x.EstimatedRecoverableBytes)) })
            .OrderByDescending(x => x.Bytes)
            .Take(8)
            .ToList();
        var max = Math.Max(1, raw.FirstOrDefault()?.Bytes ?? 1);
        ChartItemsControl.ItemsSource = raw.Select(x => new { x.Action, x.Text, BarWidth = Math.Max(8, 170.0 * x.Bytes / max) }).ToList();
    }
}
