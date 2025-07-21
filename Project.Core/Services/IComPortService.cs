using Project.Core.Models;
namespace Project.Core.Services;
public interface IComPortService
{
    event EventHandler<bool> ConnectionStatusChanged;
    event EventHandler<string> DataReceived;
    event EventHandler<string> DataWritten;
    bool IsConnected { get; }
    bool IsContinuousReadingActive { get; }
    ComPortSettings CurrentSettings { get; }
    Task<string[]> GetAvailablePortsAsync();
    Task<bool> ConnectAsync(ComPortSettings settings);
    Task DisconnectAsync();
    Task StartContinuousReadingAsync();
    Task StopContinuousReadingAsync();
    Task WriteAsync(string command);
    Task<string> SendCommandAsync(string command);
    Task<string> WriteReadAsync(string command);
    Task<string> ReadAsync();
    Task<string> SetWavelengthAsync(double wavelength);
    Task<(double Energy, string Response)> GetEnergyAsync();
    Task<string> GetDeviceIdAsync();
    Task<string> ResetDeviceAsync();
} 