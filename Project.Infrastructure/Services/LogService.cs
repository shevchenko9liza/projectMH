using Project.Core.Services;
using System.Collections.Concurrent;
using System.Text;
namespace Project.Infrastructure.Services;
public class LogService : ILogService
{
    private readonly ConcurrentQueue<string> _logEntries;
    private readonly string _logFilePath;
    private readonly object _fileLock = new();
    private const int MaxMemoryEntries = 1000;
    public event EventHandler<string>? LogEntryCreated;
    public LogService()
    {
        _logEntries = new ConcurrentQueue<string>();
        var logsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        Directory.CreateDirectory(logsDirectory);
        var fileName = $"Application_{DateTime.Now:yyyy-MM-dd}.log";
        _logFilePath = Path.Combine(logsDirectory, fileName);
    }
    public async Task LogDebugAsync(string message, string source = "Application")
    {
        await LogAsync(LogLevel.Debug, message, source);
    }
    public async Task LogInfoAsync(string message, string source = "Application")
    {
        await LogAsync(LogLevel.Info, message, source);
    }
    public async Task LogWarningAsync(string message, string source = "Application")
    {
        await LogAsync(LogLevel.Warning, message, source);
    }
    public async Task LogErrorAsync(string message, string source = "Application")
    {
        await LogAsync(LogLevel.Error, message, source);
    }
    public async Task LogErrorAsync(string message, Exception exception, string source = "Application")
    {
        var fullMessage = $"{message}\nException: {exception.GetType().Name}\nMessage: {exception.Message}\nStackTrace: {exception.StackTrace}";
        await LogAsync(LogLevel.Error, fullMessage, source);
    }
    public async Task LogCriticalAsync(string message, string source = "Application")
    {
        await LogAsync(LogLevel.Critical, message, source);
    }
    public async Task LogComOperationAsync(string operation, string data, bool isSuccess = true)
    {
        var level = isSuccess ? LogLevel.Info : LogLevel.Error;
        var status = isSuccess ? "SUCCESS" : "FAILED";
        var message = $"COM Operation: {operation} | Data: {data} | Status: {status}";
        await LogAsync(level, message, "ComPort");
    }
    public async Task<IEnumerable<string>> GetRecentLogsAsync(int count = 100)
    {
        await Task.CompletedTask; 
        var entries = _logEntries.ToArray();
        return entries.TakeLast(count);
    }
    public async Task ClearLogsAsync()
    {
        await Task.CompletedTask; 
        while (_logEntries.TryDequeue(out _)) { }
        lock (_fileLock)
        {
            File.WriteAllText(_logFilePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [INFO] [LogService] Log cleared\n");
        }
    }
    public async Task ExportLogsAsync(string filePath)
    {
        var entries = await GetRecentLogsAsync(int.MaxValue);
        var content = string.Join(Environment.NewLine, entries);   
        await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);
        await LogInfoAsync($"Logs exported to: {filePath}", "LogService");
    }
    private async Task LogAsync(LogLevel level, string message, string source)
    {
        var timestamp = DateTime.Now;
        var logEntry = FormatLogEntry(timestamp, level, source, message);
        _logEntries.Enqueue(logEntry);
        while (_logEntries.Count > MaxMemoryEntries)
        {
            _logEntries.TryDequeue(out _);
        }
        await WriteToFileAsync(logEntry);
        LogEntryCreated?.Invoke(this, logEntry);
    }
    private static string FormatLogEntry(DateTime timestamp, LogLevel level, string source, string message)
    {
        return $"[{timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{level.ToString().ToUpper()}] [{source}] {message}";
    }
    private async Task WriteToFileAsync(string logEntry)
    {
        try
        {
            await Task.Run(() =>
            {
                lock (_fileLock)
                {
                    File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to write to log file: {ex.Message}");
            Console.WriteLine($"Log entry: {logEntry}");
        }
    }
} 