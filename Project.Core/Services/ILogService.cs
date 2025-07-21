namespace Project.Core.Services;
public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error,
    Critical
}
public interface ILogService
{
    event EventHandler<string> LogEntryCreated;
    Task LogDebugAsync(string message, string source = "Application");
    Task LogInfoAsync(string message, string source = "Application");
    Task LogWarningAsync(string message, string source = "Application");
    Task LogErrorAsync(string message, string source = "Application");
    Task LogErrorAsync(string message, Exception exception, string source = "Application");
    Task LogCriticalAsync(string message, string source = "Application");
    Task LogComOperationAsync(string operation, string data, bool isSuccess = true);
    Task<IEnumerable<string>> GetRecentLogsAsync(int count = 100);
    Task ClearLogsAsync();
    Task ExportLogsAsync(string filePath);
} 