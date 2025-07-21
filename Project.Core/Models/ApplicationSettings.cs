namespace Project.Core.Models;
public class ApplicationSettings
{
    public string Theme { get; set; } = "Dark";
    public string Language { get; set; } = "en-US";
    public string WindowStartupLocation { get; set; } = "CenterScreen";
    public string WindowState { get; set; } = "Maximized";
    public bool AutoSaveSettings { get; set; } = true;
}
public class ComPortConfiguration
{
    public int DefaultBaudRate { get; set; } = 9600;
    public int ConnectionTimeout { get; set; } = 5000;
    public int ReadTimeout { get; set; } = 5000;
    public int WriteTimeout { get; set; } = 5000;
    public int DefaultDataBits { get; set; } = 8;
    public int DefaultStopBits { get; set; } = 1;
    public string DefaultParity { get; set; } = "None";
    public bool AutoConnectOnStartup { get; set; } = false;
    public string DefaultPort { get; set; } = "COM1";
}
public class DataManagementSettings
{
    public int MaxHistoryRecords { get; set; } = 50;
    public bool EnableAutoDataGeneration { get; set; } = true;
    public int DataGenerationInterval { get; set; } = 1000;
    public bool EnableDataLogging { get; set; } = true;
    public int LogRotationDays { get; set; } = 30;
}
public class MeasurementSettings
{
    public WavelengthRangeSettings WavelengthRange { get; set; } = new();
    public int StabilizationDelay { get; set; } = 500;
    public int MaxConcurrentMeasurements { get; set; } = 1;
    public bool EnableProgressTracking { get; set; } = true;
    public bool AutoClearPreviousResults { get; set; } = true;
}
public class WavelengthRangeSettings
{
    public double MinWavelength { get; set; } = 190;
    public double MaxWavelength { get; set; } = 1100;
}
public class ChartSettings
{
    public bool EnableRealTimeUpdates { get; set; } = true;
    public int MaxDataPoints { get; set; } = 1000;
    public int DefaultHeight { get; set; } = 300;
    public bool EnableAnimations { get; set; } = true;
    public int PointSize { get; set; } = 8;
    public int LineThickness { get; set; } = 3;
    public bool EnableSmoothing { get; set; } = true;
    public double SmoothnessFactor { get; set; } = 0.3;
}
public class LoggingSettings
{
    public string LogDirectory { get; set; } = "Logs";
    public string MaxLogFileSize { get; set; } = "10MB";
    public int MaxMemoryEntries { get; set; } = 1000;
    public bool EnableDebugLogging { get; set; } = true;
    public string[] LogLevels { get; set; } = { "Debug", "Info", "Warning", "Error", "Critical" };
    public bool EnableFileRotation { get; set; } = true;
    public string LogFormat { get; set; } = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level}] [{Source}] {Message}";
}
public class SecuritySettings
{
    public bool EnableAuthentication { get; set; } = true;
    public int SessionTimeout { get; set; } = 3600;
    public int MaxLoginAttempts { get; set; } = 3;
    public int PasswordMinLength { get; set; } = 8;
    public bool EnablePasswordComplexity { get; set; } = false;
}
public class PerformanceSettings
{
    public int UIUpdateInterval { get; set; } = 50;
    public int BackgroundTaskDelay { get; set; } = 100;
    public int MaxThreads { get; set; } = 4;
    public bool EnablePerformanceMonitoring { get; set; } = false;
}
public class AdvancedSettings
{
    public bool EnableDeveloperMode { get; set; } = false;
    public bool EnableExperimentalFeatures { get; set; } = false;
    public bool DebugMode { get; set; } = false;
    public bool EnableTelemetry { get; set; } = false;
} 