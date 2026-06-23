namespace SafeStorageScanner.Models;

public sealed class AppSettings
{
    public bool DarkMode { get; set; }
    public bool BeginnerMode { get; set; } = true;
    public bool AdvancedMode { get; set; }
    public bool LowStorageMode { get; set; }
    public bool DryRunMode { get; set; } = true;
    public bool PortableMode { get; set; } = true;
    public bool VerifyDuplicateContent { get; set; }
    public string Theme { get; set; } = "System";
    public string Language { get; set; } = "English";
    public string StartupBehavior { get; set; } = "Open dashboard";
    public string DefaultScanType { get; set; } = "Single drive";
    public string SingleDriveDefault { get; set; } = "";
    public string FolderDefault { get; set; } = "";
    public bool ScanHiddenFiles { get; set; }
    public bool ScanSystemFiles { get; set; }
    public bool ScanCompressedArchives { get; set; }
    public string DuplicateDetectionMode { get; set; } = "Size + hash";
    public long LowStorageThresholdGb { get; set; } = 25;
    public int FileAgeDays { get; set; } = 365;
    public long RecommendationThresholdMb { get; set; } = 50;
    public int ConfidenceThreshold { get; set; } = 60;
    public int RiskThreshold { get; set; } = 70;
    public string MoveDefaultDestination { get; set; } = "";
    public bool PreserveFolderStructure { get; set; } = true;
    public bool VerifyFreeSpaceBeforeMove { get; set; } = true;
    public bool CopyInsteadOfMove { get; set; }
    public bool EnableAiAnalysis { get; set; }
    public string AiApiKey { get; set; } = "";
    public bool ExplainRecommendationsAutomatically { get; set; }
    public string RecommendationAggressiveness { get; set; } = "Conservative";
    public string ConfirmationRequirements { get; set; } = "Always confirm";
    public bool CreateRestorePoint { get; set; }
    public bool AuditLogging { get; set; } = true;
    public bool DeletionSafeguards { get; set; } = true;
    public string DefaultChartType { get; set; } = "Treemap";
    public string TreemapSettings { get; set; } = "Largest folders first";
    public string DashboardWidgets { get; set; } = "Drives, Quick stats, Largest folders";
    public string ExportFormats { get; set; } = "CSV, JSON, HTML, PDF";
    public bool AutomaticReportGeneration { get; set; }
    public int ReportRetentionDays { get; set; } = 90;
    public int ThreadCount { get; set; } = 4;
    public string CacheLocation { get; set; } = "";
    public string DatabaseLocation { get; set; } = "";
    public bool DebugLogging { get; set; }
    public List<string> TargetDrives { get; set; } = [];
    public List<string> ScanLocations { get; set; } = [];
    public List<string> Exclusions { get; set; } = [];
    public List<ScanProfile> Profiles { get; set; } = [];
    public string CloudAiProvider { get; set; } = "Disabled";
    public bool PreferLocalAi { get; set; } = true;
    public string ScheduleInterval { get; set; } = "Manual";
}

public sealed class ScanProfile
{
    public string Name { get; set; } = "Default";
    public List<string> TargetDrives { get; set; } = [];
    public List<string> ScanLocations { get; set; } = [];
    public long RecommendationThresholdMb { get; set; } = 50;
    public int FileAgeDays { get; set; } = 365;
}
