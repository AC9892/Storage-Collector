using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using SafeStorageScanner.Models;
using SafeStorageScanner.Services;

namespace SafeStorageScanner;

public partial class SettingsWindow : Window
{
    private readonly SettingsService _settingsService;
    private AppSettings _settings = new();

    public SettingsWindow(SettingsService settingsService)
    {
        InitializeComponent();
        _settingsService = settingsService;
        _settings = _settingsService.Load();
        Loaded += (_, _) => ThemeManager.ApplyToWindow(this);
        PreviewMouseWheel += SettingsWindow_PreviewMouseWheel;
        LoadDrives();
        LoadSettings();
    }

    private void SettingsWindow_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (IsComboBoxDropDownOpen(e.OriginalSource as DependencyObject))
        {
            return;
        }

        var scrollViewer = FindAncestor<ScrollViewer>(e.OriginalSource as DependencyObject);
        if (scrollViewer is null)
        {
            return;
        }

        if (e.Delta < 0)
        {
            scrollViewer.LineDown();
            scrollViewer.LineDown();
            scrollViewer.LineDown();
        }
        else
        {
            scrollViewer.LineUp();
            scrollViewer.LineUp();
            scrollViewer.LineUp();
        }

        e.Handled = true;
    }

    private void LoadDrives()
    {
        var drives = DriveInfo.GetDrives().Where(d => d.IsReady).Select(d => d.RootDirectory.FullName).ToList();
        SingleDriveComboBox.ItemsSource = drives;
        TargetDrivesListBox.ItemsSource = drives;
    }

    private void LoadSettings()
    {
        SelectCombo(ThemeComboBox, _settings.Theme);
        SelectCombo(LanguageComboBox, _settings.Language);
        SelectCombo(StartupComboBox, _settings.StartupBehavior);
        PortableModeCheckBox.IsChecked = _settings.PortableMode;
        SelectCombo(DefaultScanTypeComboBox, _settings.DefaultScanType);
        SelectCombo(SingleDriveComboBox, _settings.SingleDriveDefault);
        FolderDefaultTextBox.Text = _settings.FolderDefault;
        SelectList(TargetDrivesListBox, _settings.TargetDrives);
        ScanLocationsTextBox.Text = string.Join(Environment.NewLine, _settings.ScanLocations);
        ExclusionsTextBox.Text = string.Join(Environment.NewLine, _settings.Exclusions);
        ScanHiddenFilesCheckBox.IsChecked = _settings.ScanHiddenFiles;
        ScanSystemFilesCheckBox.IsChecked = _settings.ScanSystemFiles;
        ScanCompressedArchivesCheckBox.IsChecked = _settings.ScanCompressedArchives;
        SelectCombo(DuplicateModeComboBox, _settings.DuplicateDetectionMode);
        LowStorageThresholdTextBox.Text = _settings.LowStorageThresholdGb.ToString();
        MinimumFileSizeTextBox.Text = _settings.RecommendationThresholdMb.ToString();
        FileAgeTextBox.Text = _settings.FileAgeDays.ToString();
        ConfidenceThresholdTextBox.Text = _settings.ConfidenceThreshold.ToString();
        RiskThresholdTextBox.Text = _settings.RiskThreshold.ToString();
        MoveDestinationTextBox.Text = _settings.MoveDefaultDestination;
        PreserveStructureCheckBox.IsChecked = _settings.PreserveFolderStructure;
        VerifyFreeSpaceCheckBox.IsChecked = _settings.VerifyFreeSpaceBeforeMove;
        CopyInsteadCheckBox.IsChecked = _settings.CopyInsteadOfMove;
        EnableAiCheckBox.IsChecked = _settings.EnableAiAnalysis;
        LocalAiCheckBox.IsChecked = _settings.PreferLocalAi;
        CloudAiCheckBox.IsChecked = !string.IsNullOrWhiteSpace(_settings.AiApiKey);
        AiApiKeyBox.Password = _settings.AiApiKey;
        ExplainAutomaticallyCheckBox.IsChecked = _settings.ExplainRecommendationsAutomatically;
        SelectCombo(AggressivenessComboBox, _settings.RecommendationAggressiveness);
        DryRunCheckBox.IsChecked = _settings.DryRunMode;
        SelectCombo(ConfirmationComboBox, _settings.ConfirmationRequirements);
        RestorePointCheckBox.IsChecked = _settings.CreateRestorePoint;
        AuditLoggingCheckBox.IsChecked = _settings.AuditLogging;
        DeletionSafeguardsCheckBox.IsChecked = _settings.DeletionSafeguards;
        SelectCombo(ChartTypeComboBox, _settings.DefaultChartType);
        TreemapSettingsTextBox.Text = _settings.TreemapSettings;
        DashboardWidgetsTextBox.Text = _settings.DashboardWidgets;
        ExportFormatsTextBox.Text = _settings.ExportFormats;
        AutomaticReportsCheckBox.IsChecked = _settings.AutomaticReportGeneration;
        ReportRetentionTextBox.Text = _settings.ReportRetentionDays.ToString();
        ThreadCountTextBox.Text = _settings.ThreadCount.ToString();
        CacheLocationTextBox.Text = _settings.CacheLocation;
        DatabaseLocationTextBox.Text = _settings.DatabaseLocation;
        DebugLoggingCheckBox.IsChecked = _settings.DebugLogging;
    }

    private void SaveSettings()
    {
        _settings.Theme = ComboText(ThemeComboBox, "System");
        _settings.DarkMode = _settings.Theme == "Dark";
        _settings.Language = ComboText(LanguageComboBox, "English");
        _settings.StartupBehavior = ComboText(StartupComboBox, "Open dashboard");
        _settings.PortableMode = PortableModeCheckBox.IsChecked == true;
        _settings.DefaultScanType = ComboText(DefaultScanTypeComboBox, "Single drive");
        _settings.SingleDriveDefault = SingleDriveComboBox.Text;
        _settings.FolderDefault = FolderDefaultTextBox.Text;
        _settings.TargetDrives = TargetDrivesListBox.SelectedItems.Cast<object>().Select(i => i.ToString()!).ToList();
        _settings.ScanLocations = Lines(ScanLocationsTextBox.Text);
        _settings.Exclusions = Lines(ExclusionsTextBox.Text);
        _settings.ScanHiddenFiles = ScanHiddenFilesCheckBox.IsChecked == true;
        _settings.ScanSystemFiles = ScanSystemFilesCheckBox.IsChecked == true;
        _settings.ScanCompressedArchives = ScanCompressedArchivesCheckBox.IsChecked == true;
        _settings.DuplicateDetectionMode = ComboText(DuplicateModeComboBox, "Size + hash");
        _settings.VerifyDuplicateContent = _settings.DuplicateDetectionMode.Contains("content", StringComparison.OrdinalIgnoreCase);
        _settings.LowStorageThresholdGb = ParseLong(LowStorageThresholdTextBox.Text, 25);
        _settings.RecommendationThresholdMb = ParseLong(MinimumFileSizeTextBox.Text, 50);
        _settings.FileAgeDays = ParseInt(FileAgeTextBox.Text, 365);
        _settings.ConfidenceThreshold = ParseInt(ConfidenceThresholdTextBox.Text, 60);
        _settings.RiskThreshold = ParseInt(RiskThresholdTextBox.Text, 70);
        _settings.MoveDefaultDestination = MoveDestinationTextBox.Text;
        _settings.PreserveFolderStructure = PreserveStructureCheckBox.IsChecked == true;
        _settings.VerifyFreeSpaceBeforeMove = VerifyFreeSpaceCheckBox.IsChecked == true;
        _settings.CopyInsteadOfMove = CopyInsteadCheckBox.IsChecked == true;
        _settings.AiApiKey = AiApiKeyBox.Password;
        _settings.EnableAiAnalysis = EnableAiCheckBox.IsChecked == true && (!CloudAiCheckBox.IsChecked == true || !string.IsNullOrWhiteSpace(_settings.AiApiKey));
        _settings.PreferLocalAi = LocalAiCheckBox.IsChecked == true;
        _settings.CloudAiProvider = string.IsNullOrWhiteSpace(_settings.AiApiKey) ? "Disabled" : "User API key configured";
        _settings.ExplainRecommendationsAutomatically = ExplainAutomaticallyCheckBox.IsChecked == true;
        _settings.RecommendationAggressiveness = ComboText(AggressivenessComboBox, "Conservative");
        _settings.DryRunMode = DryRunCheckBox.IsChecked == true;
        _settings.ConfirmationRequirements = ComboText(ConfirmationComboBox, "Always confirm");
        _settings.CreateRestorePoint = RestorePointCheckBox.IsChecked == true;
        _settings.AuditLogging = AuditLoggingCheckBox.IsChecked == true;
        _settings.DeletionSafeguards = DeletionSafeguardsCheckBox.IsChecked == true;
        _settings.DefaultChartType = ComboText(ChartTypeComboBox, "Treemap");
        _settings.TreemapSettings = TreemapSettingsTextBox.Text;
        _settings.DashboardWidgets = DashboardWidgetsTextBox.Text;
        _settings.ExportFormats = ExportFormatsTextBox.Text;
        _settings.AutomaticReportGeneration = AutomaticReportsCheckBox.IsChecked == true;
        _settings.ReportRetentionDays = ParseInt(ReportRetentionTextBox.Text, 90);
        _settings.ThreadCount = ParseInt(ThreadCountTextBox.Text, 4);
        _settings.CacheLocation = CacheLocationTextBox.Text;
        _settings.DatabaseLocation = DatabaseLocationTextBox.Text;
        _settings.DebugLogging = DebugLoggingCheckBox.IsChecked == true;
        _settingsService.Save(_settings);
    }

    private void SettingsNavList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SettingsTabs is null || SettingsTitleTextBlock is null || SettingsSubtitleTextBlock is null || SettingsNavList.SelectedIndex < 0)
        {
            return;
        }

        SettingsTabs.SelectedIndex = SettingsNavList.SelectedIndex;
        (SettingsTitleTextBlock.Text, SettingsSubtitleTextBlock.Text) = SettingsNavList.SelectedIndex switch
        {
            0 => ("General", "Personalize the app experience."),
            1 => ("Scanning", "Set scan defaults, locations, exclusions, and duplicate detection."),
            2 => ("Storage Analysis", "Tune thresholds for cleanup recommendations."),
            3 => ("Move Instead of Delete", "Configure relocation behavior."),
            4 => ("AI Analysis", "Configure local and optional cloud-assisted explanations."),
            5 => ("Safety", "Control confirmations, audit logging, and safeguards."),
            6 => ("Visualization", "Choose chart and dashboard defaults."),
            7 => ("Reports", "Configure report output and retention."),
            8 => ("Advanced", "Diagnostics and storage locations."),
            _ => ("Settings", "")
        };
        Dispatcher.BeginInvoke(() => ThemeManager.ApplyToWindow(this), System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemeComboBox is null)
        {
            return;
        }

        var selectedTheme = ComboText(ThemeComboBox, _settings.Theme);
        ThemeManager.ApplyTheme(selectedTheme.Equals("Dark", StringComparison.OrdinalIgnoreCase) ? AppTheme.Dark : AppTheme.Light);
    }

    private void UseSelectedDrives_Click(object sender, RoutedEventArgs e)
    {
        var drives = TargetDrivesListBox.SelectedItems.Cast<object>().Select(i => i.ToString()!).ToList();
        ScanLocationsTextBox.Text = string.Join(Environment.NewLine, CommonLocationService.GetCommonLocations(drives).Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        SaveSettings();
        ThemeManager.ApplyTheme(_settings.Theme.Equals("Dark", StringComparison.OrdinalIgnoreCase) || _settings.DarkMode ? AppTheme.Dark : AppTheme.Light);
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private static void SelectCombo(System.Windows.Controls.ComboBox combo, string value)
    {
        foreach (var item in combo.Items)
        {
            var text = item is ComboBoxItem c ? c.Content?.ToString() : item?.ToString();
            if (string.Equals(text, value, StringComparison.OrdinalIgnoreCase))
            {
                combo.SelectedItem = item;
                return;
            }
        }
        combo.Text = value;
    }

    private static void SelectList(System.Windows.Controls.ListBox list, IEnumerable<string> values)
    {
        var set = values.ToHashSet(StringComparer.OrdinalIgnoreCase);
        list.SelectedItems.Clear();
        foreach (var item in list.Items)
        {
            if (set.Contains(item.ToString() ?? ""))
            {
                list.SelectedItems.Add(item);
            }
        }
    }

    private static string ComboText(System.Windows.Controls.ComboBox combo, string fallback)
    {
        return (combo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? combo.Text.NullIfWhiteSpace() ?? fallback;
    }

    private static List<string> Lines(string text) => text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    private static int ParseInt(string text, int fallback) => int.TryParse(text, out var value) ? value : fallback;
    private static long ParseLong(string text, long fallback) => long.TryParse(text, out var value) ? Math.Max(0, value) : fallback;

    private static bool IsComboBoxDropDownOpen(DependencyObject? source)
    {
        var combo = FindAncestor<System.Windows.Controls.ComboBox>(source);
        return combo?.IsDropDownOpen == true || FindAncestor<Popup>(source) is not null;
    }

    private static T? FindAncestor<T>(DependencyObject? source) where T : DependencyObject
    {
        while (source is not null)
        {
            if (source is T match)
            {
                return match;
            }

            source = source is Visual or Visual3D
                ? VisualTreeHelper.GetParent(source)
                : LogicalTreeHelper.GetParent(source);
        }

        return null;
    }
}

internal static class SettingsStringExtensions
{
    public static string? NullIfWhiteSpace(this string value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
