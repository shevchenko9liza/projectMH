using Project.Core.Models;
using Project.Core.Services;
using System.IO.Ports;
using System.Text;
using System.Text.RegularExpressions;
namespace Project.Infrastructure.Services;
public class ComPortService : IComPortService, IDisposable
{
    private SerialPort? _serialPort;
    private readonly ILogService _logService;
    private ComPortSettings _currentSettings;
    private readonly object _portLock = new();
    private bool _disposed = false;
    private CancellationTokenSource? _readingCancellationTokenSource;
    private Task? _continuousReadingTask;
    private bool _isContinuousReadingActive = false;
    private readonly SemaphoreSlim _commandSemaphore = new(1, 1);
    public event EventHandler<bool>? ConnectionStatusChanged;
    public event EventHandler<string>? DataReceived;
    public event EventHandler<string>? DataWritten;
    public bool IsConnected => _serialPort?.IsOpen ?? false;
    public bool IsContinuousReadingActive => _isContinuousReadingActive;
    public ComPortSettings CurrentSettings => _currentSettings;
    public ComPortService(ILogService logService)
    {
        _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        _currentSettings = new ComPortSettings();
    }
    public async Task<string[]> GetAvailablePortsAsync()
    {
        try
        {
            await _logService.LogDebugAsync("Getting available COM ports", "ComPortService");
            var ports = await Task.Run(() => SerialPort.GetPortNames());
            await _logService.LogInfoAsync($"Found {ports.Length} available ports: {string.Join(", ", ports)}", "ComPortService");
            return ports;
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync("Failed to get available ports", ex, "ComPortService");
            return Array.Empty<string>();
        }
    }
    public async Task<bool> ConnectAsync(ComPortSettings settings)
    {
        try
        {
            if (IsConnected)
            {
                await _logService.LogWarningAsync($"Already connected to {_currentSettings.PortName}", "ComPortService");
                return true;
            }
            await _logService.LogInfoAsync($"Attempting to connect to {settings.PortName} at {settings.BaudRate} baud", "ComPortService");
            await Task.Run(() =>
            {
                lock (_portLock)
                {
                    _serialPort = new SerialPort
                    {
                        PortName = settings.PortName,
                        BaudRate = settings.BaudRate,
                        DataBits = settings.DataBits,
                        StopBits = (StopBits)settings.StopBits,
                        Parity = Enum.Parse<Parity>(settings.Parity),
                        ReadTimeout = settings.ReadTimeout,
                        WriteTimeout = settings.WriteTimeout,
                        Handshake = Handshake.None,
                        RtsEnable = true,
                        DtrEnable = true
                    };
                    _serialPort.Open();
                }
            });
            if (IsConnected)
            {
                _currentSettings = settings;
                _currentSettings.IsConnected = true;
                await _logService.LogInfoAsync($"Successfully connected to {settings.PortName}", "ComPortService");
                await _logService.LogComOperationAsync("Connect", settings.PortName, true);
                ConnectionStatusChanged?.Invoke(this, true);
                await StartContinuousReadingAsync();
                return true;
            }
            await _logService.LogErrorAsync($"Failed to connect to {settings.PortName}", "ComPortService");
            return false;
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync($"Connection error to {settings.PortName}: {ex.Message}", ex, "ComPortService");
            await _logService.LogComOperationAsync("Connect", settings.PortName, false);
            return false;
        }
    }
    public async Task DisconnectAsync()
    {
        try
        {
            await StopContinuousReadingAsync();
            if (_serialPort?.IsOpen == true)
            {
                await _logService.LogInfoAsync($"Disconnecting from {_currentSettings.PortName}", "ComPortService");
                await Task.Run(() =>
                {
                    lock (_portLock)
                    {
                        _serialPort?.Close();
                        _serialPort?.Dispose();
                        _serialPort = null;
                    }
                });
                await _logService.LogComOperationAsync("Disconnect", _currentSettings.PortName, true);
            }
            _currentSettings.IsConnected = false;
            ConnectionStatusChanged?.Invoke(this, false);
            await _logService.LogInfoAsync("Successfully disconnected", "ComPortService");
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync("Error during disconnect", ex, "ComPortService");
            await _logService.LogComOperationAsync("Disconnect", _currentSettings.PortName, false);
        }
    }
    public async Task StartContinuousReadingAsync()
    {
        if (!IsConnected)
        {
            await _logService.LogWarningAsync("Cannot start continuous reading: not connected", "ComPortService");
            return;
        }
        if (_isContinuousReadingActive)
        {
            await _logService.LogWarningAsync("Continuous reading already active", "ComPortService");
            return;
        }
        try
        {
            _readingCancellationTokenSource = new CancellationTokenSource();
            _isContinuousReadingActive = true;
            await _logService.LogInfoAsync("Starting continuous reading task", "ComPortService");
            _continuousReadingTask = Task.Run(async () =>
            {
                await ContinuousReadingLoop(_readingCancellationTokenSource.Token);
            }, _readingCancellationTokenSource.Token);

            await _logService.LogInfoAsync("Continuous reading started successfully", "ComPortService");
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync("Failed to start continuous reading", ex, "ComPortService");
            _isContinuousReadingActive = false;
        }
    }
    public async Task StopContinuousReadingAsync()
    {
        if (!_isContinuousReadingActive)
        {
            return;
        }
        try
        {
            await _logService.LogInfoAsync("Stopping continuous reading", "ComPortService");
            _readingCancellationTokenSource?.Cancel();
            if (_continuousReadingTask != null)
            {
                await _continuousReadingTask;
            }
            _readingCancellationTokenSource?.Dispose();
            _readingCancellationTokenSource = null;
            _continuousReadingTask = null;
            _isContinuousReadingActive = false;
            await _logService.LogInfoAsync("Continuous reading stopped successfully", "ComPortService");
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync("Error stopping continuous reading", ex, "ComPortService");
        }
    }
    public async Task WriteAsync(string command)
    {
        if (!IsConnected)
        {
            var error = "Cannot write: not connected to device";
            await _logService.LogErrorAsync(error, "ComPortService");
            throw new InvalidOperationException(error);
        }
        try
        {
            await _logService.LogDebugAsync($"Writing command: {command}", "ComPortService");
            await Task.Run(() =>
            {
                lock (_portLock)
                {
                    _serialPort?.WriteLine(command);
                }
            });
            await _logService.LogComOperationAsync("Write", command, true);
            DataWritten?.Invoke(this, command);
            await _logService.LogInfoAsync($"Successfully wrote command: {command}", "ComPortService");
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync($"Write operation failed for command: {command}", ex, "ComPortService");
            await _logService.LogComOperationAsync("Write", command, false);
            throw;
        }
    }
    public async Task<string> SendCommandAsync(string command)
    {
        if (!IsConnected)
        {
            var error = "Cannot send command: not connected to device";
            await _logService.LogErrorAsync(error, "ComPortService");
            throw new InvalidOperationException(error);
        }
        await _logService.LogDebugAsync($"Acquiring semaphore for command: {command}", "ComPortService");
        await _commandSemaphore.WaitAsync();
        try
        {
            await _logService.LogInfoAsync($"Semaphore acquired, executing command: {command}", "ComPortService");
            bool wasReadingActive = _isContinuousReadingActive;
            if (wasReadingActive)
            {
                await StopContinuousReadingAsync();
                await Task.Delay(100); 
            }
            await _logService.LogDebugAsync($"Writing synchronized command: {command}", "ComPortService");
            await Task.Run(() =>
            {
                lock (_portLock)
                {
                    _serialPort?.WriteLine(command);
                }
            });
            await _logService.LogComOperationAsync("SyncWrite", command, true);
            DataWritten?.Invoke(this, command);
            await _logService.LogDebugAsync($"Reading response for command: {command}", "ComPortService");         
            var response = await Task.Run(() =>
            {
                lock (_portLock)
                {
                    return _serialPort?.ReadLine() ?? string.Empty;
                }
            });
            await _logService.LogComOperationAsync("SyncRead", response, true);
            await _logService.LogInfoAsync($"Command completed successfully. Command: {command}, Response: {response}", "ComPortService");
            if (wasReadingActive)
            {
                await StartContinuousReadingAsync();
            }
            return response;
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync($"Synchronized command execution failed for: {command}", ex, "ComPortService");
            await _logService.LogComOperationAsync("SyncCommand", $"Error: {ex.Message}", false);
            throw;
        }
        finally
        {
            _commandSemaphore.Release();
            await _logService.LogDebugAsync($"Semaphore released for command: {command}", "ComPortService");
        }
    }
    public async Task<string> ReadAsync()
    {
        if (!IsConnected)
        {
            var error = "Cannot read: not connected to device";
            await _logService.LogErrorAsync(error, "ComPortService");
            throw new InvalidOperationException(error);
        }
        try
        {
            await _logService.LogDebugAsync("Reading from device", "ComPortService");
            var response = await Task.Run(() =>
            {
                lock (_portLock)
                {
                    return _serialPort?.ReadLine() ?? string.Empty;
                }
            });
            await _logService.LogComOperationAsync("Read", response, true);
            await _logService.LogInfoAsync($"Successfully read response: {response}", "ComPortService");
            return response;
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync("Read operation failed", ex, "ComPortService");
            await _logService.LogComOperationAsync("Read", "Error", false);
            throw;
        }
    }
    public async Task<string> WriteReadAsync(string command)
    {
        if (!IsConnected)
        {
            var error = "Cannot send command: not connected to device";
            await _logService.LogErrorAsync(error, "ComPortService");
            throw new InvalidOperationException(error);
        }
        try
        {
            bool wasReadingActive = _isContinuousReadingActive;
            if (wasReadingActive)
            {
                await StopContinuousReadingAsync();
                await Task.Delay(100); 
            }
            await WriteAsync(command);
            var response = await ReadAsync();
            if (wasReadingActive)
            {
                await StartContinuousReadingAsync();
            }
            return response;
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync($"WriteRead operation failed for command: {command}", ex, "ComPortService");
            throw;
        }
    }
    public async Task<string> SetWavelengthAsync(double wavelength)
    {
        if (wavelength < 190 || wavelength > 1100)
        {
            var error = $"Wavelength {wavelength} nm is out of range (190-1100 nm)";
            await _logService.LogErrorAsync(error, "ComPortService");
            throw new ArgumentOutOfRangeException(nameof(wavelength), error);
        }

        var command = $"WAVELENGTH {wavelength:F1}";
        return await WriteReadAsync(command);
    }
    public async Task<(double Energy, string Response)> GetEnergyAsync()
    {
        var response = await WriteReadAsync("ENERGY?");
        var match = Regex.Match(response, @"(\d+\.?\d*)\s*(mJ|J|uJ)");
        double energy = 0;   
        if (match.Success && double.TryParse(match.Groups[1].Value, out energy))
        {
            var unit = match.Groups[2].Value.ToLower();
            energy = unit switch
            {
                "j" => energy * 1000,
                "uj" => energy / 1000,
                _ => energy 
            };
        }
        return (energy, response);
    }
    public async Task<string> GetDeviceIdAsync()
    {
        return await WriteReadAsync("*IDN?");
    }
    public async Task<string> ResetDeviceAsync()
    {
        return await WriteReadAsync("*RST");
    }
    private async Task ContinuousReadingLoop(CancellationToken cancellationToken)
    {
        await _logService.LogDebugAsync("Starting continuous reading loop", "ComPortService");
        while (!cancellationToken.IsCancellationRequested && IsConnected)
        {
            try
            {
                string? data = null;
                await Task.Run(() =>
                {
                    lock (_portLock)
                    {
                        if (_serialPort?.IsOpen == true && _serialPort.BytesToRead > 0)
                        {
                            try
                            {
                                data = _serialPort.ReadExisting();
                            }
                            catch (TimeoutException)
                            {
                            }
                            catch (Exception ex)
                            {
                                _logService.LogWarningAsync($"Read error in continuous loop: {ex.Message}", "ComPortService");
                            }
                        }
                    }
                }, cancellationToken);
                if (!string.IsNullOrWhiteSpace(data))
                {
                    await _logService.LogDebugAsync($"Continuous read received: {data.Trim()}", "ComPortService");
                    var lines = data.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            DataReceived?.Invoke(this, line.Trim());
                            await _logService.LogComOperationAsync("ContinuousRead", line.Trim(), true);
                        }
                    }
                }
                await Task.Delay(50, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync("Error in continuous reading loop", ex, "ComPortService");
                try
                {
                    await Task.Delay(1000, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
        await _logService.LogDebugAsync("Continuous reading loop ended", "ComPortService");
    }
    public void Dispose()
    {
        if (!_disposed)
        {
            _readingCancellationTokenSource?.Cancel();
            _continuousReadingTask?.Wait(TimeSpan.FromSeconds(2));
            _readingCancellationTokenSource?.Dispose();
            _commandSemaphore?.Dispose();       
            lock (_portLock)
            {
                _serialPort?.Close();
                _serialPort?.Dispose();
            }
            _disposed = true;
        }
    }
} 