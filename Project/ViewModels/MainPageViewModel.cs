using Project.Core.Models;
using Project.Core.Services;
using Project.Infrastructure.Services;
using Project.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows;
using LiveChartsCore;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.IO;
using System.Linq;
namespace Project.ViewModels;
public class MainPageViewModel : BaseViewModel
{
    #region Private Fields
    private readonly INavigationService _navigationService;
    private readonly ILogService _logService;
    private readonly IComPortService _comPortService;
    private readonly DispatcherTimer _dataTimer;
    private bool _isAutoAdding = true;
    private string _latestData = "Waiting for data...";
    private string _manualEntryText = string.Empty;
    private int _dataCounter = 0;
    private string _selectedPort = string.Empty;
    private int _baudRate = 9600;
    private string _commandText = string.Empty;
    private double _wavelength = 500.0;
    private bool _isConnected = false;
    private string _connectionStatus = "Disconnected";
    private string _wavelengthsInput = string.Empty;
    private bool _isMeasuring = false;
    private double _measurementProgress = 0.0;
    private string _measurementStatus = "Ready for measurements";
    private CancellationTokenSource? _measurementCancellationTokenSource;
    private int _measurementCounter = 0;
    private readonly ObservableCollection<ObservablePoint> _chartDataPoints = new();
    private ISeries[]? _energyChartSeries;

    #endregion

    #region Public Properties
    public ObservableCollection<DataItem> DataItems { get; } = new();
    public ObservableCollection<string> AvailablePorts { get; } = new();
    public ObservableCollection<int> AvailableBaudRates { get; } = new() { 9600, 19200, 38400, 57600, 115200 };
    public ObservableCollection<string> AvailableCommands { get; } = new() 
    { 
        "ENERGY?", 
        "WAVELENGTH", 
        "*RST", 
        "STATUS?", 
        "VERSION?", 
        "POWER?", 
        "INTENSITY?", 
        "CALIBRATE", 
        "AUTOZERO", 
        "CONFIG?" 
    };
    public bool IsAutoAdding
    {
        get => _isAutoAdding;
        set
        {
            if (SetProperty(ref _isAutoAdding, value))
            {
                OnPropertyChanged(nameof(AutoAddingStatus));
                OnPropertyChanged(nameof(CanManualAdd));
            }
        }
    }
    public string AutoAddingStatus => IsAutoAdding ? "Running" : "Stopped";
    public bool CanManualAdd => !IsAutoAdding;
    public string LatestData
    {
        get => _latestData;
        set => SetProperty(ref _latestData, value);
    }
    public string ManualEntryText
    {
        get => _manualEntryText;
        set => SetProperty(ref _manualEntryText, value);
    }
    public string SelectedPort
    {
        get => _selectedPort;
        set
        {
            if (SetProperty(ref _selectedPort, value))
            {
                RaiseCanExecuteChanged();
            }
        }
    }
    public int BaudRate
    {
        get => _baudRate;
        set
        {
            if (SetProperty(ref _baudRate, value))
            {
                RaiseCanExecuteChanged();
            }
        }
    }
    public string CommandText
    {
        get => _commandText;
        set
        {
            if (SetProperty(ref _commandText, value))
            {
                RaiseCanExecuteChanged();
            }
        }
    }
    public double Wavelength
    {
        get => _wavelength;
        set
        {
            if (SetProperty(ref _wavelength, value))
            {
                OnPropertyChanged(nameof(CanSetWavelength));
                RaiseCanExecuteChanged();
            }
        }
    }
    public bool IsConnected
    {
        get => _isConnected;
        set
        {
            if (SetProperty(ref _isConnected, value))
            {
                OnPropertyChanged(nameof(CanConnect));
                OnPropertyChanged(nameof(CanDisconnect));
                OnPropertyChanged(nameof(CanSendCommand));
                OnPropertyChanged(nameof(CanSetWavelength));
                OnPropertyChanged(nameof(CanGetEnergy));
                OnPropertyChanged(nameof(CanResetDevice));
                OnPropertyChanged(nameof(CanMeasureMultiple));
                RaiseCanExecuteChanged();
            }
        }
    }
    public string ConnectionStatus
    {
        get => _connectionStatus;
        set => SetProperty(ref _connectionStatus, value);
    }
    public bool CanConnect => !IsConnected;
    public bool CanDisconnect => IsConnected;
    public bool CanSendCommand => IsConnected && !string.IsNullOrWhiteSpace(CommandText);
    public bool CanSetWavelength => IsConnected && Wavelength >= 190 && Wavelength <= 1100;
    public bool CanGetEnergy => IsConnected;
    public bool CanResetDevice => IsConnected;
    public ObservableCollection<MeasurementResult> MeasurementResults { get; } = new();
    public string WavelengthsInput
    {
        get => _wavelengthsInput;
        set
        {
            if (SetProperty(ref _wavelengthsInput, value))
            {
                RaiseCanExecuteChanged();
            }
        }
    }
    public bool IsMeasuring
    {
        get => _isMeasuring;
        set
        {
            if (SetProperty(ref _isMeasuring, value))
            {
                RaiseCanExecuteChanged();
            }
        }
    }
    public double MeasurementProgress
    {
        get => _measurementProgress;
        set => SetProperty(ref _measurementProgress, value);
    }
    public string MeasurementStatus
    {
        get => _measurementStatus;
        set => SetProperty(ref _measurementStatus, value);
    }
    public bool CanMeasureMultiple => IsConnected && !string.IsNullOrWhiteSpace(WavelengthsInput) && !IsMeasuring;
    public bool CanCancelMeasurement => IsMeasuring;
    public ISeries[] EnergyChartSeries
    {
        get
        {
            if (_energyChartSeries == null)
            {
                InitializeChart();
            }
            return _energyChartSeries ?? Array.Empty<ISeries>();
        }
    }
    #endregion
    #region Commands
        public ICommand LogoutCommand { get; }
    public ICommand StopThreadCommand { get; }
    public ICommand ResumeThreadCommand { get; }
    public ICommand ManualAddCommand { get; }
    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }
    public ICommand SendCommandCommand { get; }
    public ICommand SetWavelengthCommand { get; }
    public ICommand GetEnergyCommand { get; }
    public ICommand RefreshPortsCommand { get; }
    public ICommand ResetDeviceCommand { get; }
    public ICommand MeasureMultipleCommand { get; }
    public ICommand CancelMeasurementCommand { get; }
    #endregion
    #region Constructors
        public MainPageViewModel() : this(new NavigationService())
        {
        }
        public MainPageViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
        _logService = new LogService();
        _comPortService = new ComPortService(_logService);
        _comPortService.ConnectionStatusChanged += OnConnectionStatusChanged;
        _comPortService.DataReceived += OnDataReceived;
        _comPortService.DataWritten += OnDataWritten;
            LogoutCommand = new RelayCommand(ExecuteLogout);
        StopThreadCommand = new RelayCommand(ExecuteStopThread, CanExecuteStopThread);
        ResumeThreadCommand = new RelayCommand(ExecuteResumeThread, CanExecuteResumeThread);
        ManualAddCommand = new RelayCommand(ExecuteManualAdd, CanExecuteManualAdd);
        ConnectCommand = new RelayCommand(ExecuteConnect);
        DisconnectCommand = new RelayCommand(ExecuteDisconnect);
        SendCommandCommand = new RelayCommand(ExecuteSendCommand);
        SetWavelengthCommand = new RelayCommand(ExecuteSetWavelength);
        GetEnergyCommand = new RelayCommand(ExecuteGetEnergy);
        RefreshPortsCommand = new RelayCommand(ExecuteRefreshPorts);
        ResetDeviceCommand = new RelayCommand(ExecuteResetDevice);
        MeasureMultipleCommand = new RelayCommand(ExecuteMeasureMultiple);
        CancelMeasurementCommand = new RelayCommand(ExecuteCancelMeasurement);
        _dataTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _dataTimer.Tick += OnDataTimerTick;
        _dataTimer.Start();
        _ = Task.Run(ExecuteRefreshPortsAsync);
        _ = Task.Run(() => _logService.LogInfoAsync("Application started with async COM operations", "MainPageViewModel"));
    }
    #endregion
    #region Command Wrappers
    private void ExecuteConnect(object? parameter)
    {
        _ = Task.Run(ExecuteConnectAsync);
    }
    
    private void ExecuteDisconnect(object? parameter)
    {
        _ = Task.Run(ExecuteDisconnectAsync);
    }
    
    private void ExecuteSendCommand(object? parameter)
    {
        _ = Task.Run(ExecuteSendCommandAsync);
    }
    
    private void ExecuteSetWavelength(object? parameter)
    {
        _ = Task.Run(ExecuteSetWavelengthAsync);
    }
    
    private void ExecuteGetEnergy(object? parameter)
    {
        _ = Task.Run(ExecuteGetEnergyAsync);
    }
    
    private void ExecuteRefreshPorts(object? parameter)
    {
        _ = Task.Run(ExecuteRefreshPortsAsync);
    }
    
    private void ExecuteResetDevice(object? parameter)
    {
        _ = Task.Run(ExecuteResetDeviceAsync);
    }
    
    private void ExecuteMeasureMultiple(object? parameter)
    {
        _ = Task.Run(ExecuteMeasureMultipleAsync);
    }
    #endregion
    #region Timer and Manual
    private void OnDataTimerTick(object? sender, EventArgs e)
    {
        if (!IsAutoAdding) return;
        _dataCounter++;
        var newItem = new DataItem
        {
            Id = _dataCounter,
            OperationType = "Auto Data",
            Content = $"Auto Data {_dataCounter}",
            Timestamp = DateTime.Now
        };
        Application.Current.Dispatcher.Invoke(() =>
        {
            DataItems.Add(newItem);
            LatestData = newItem.FullDescription;
            if (DataItems.Count > 50)
            {
                DataItems.RemoveAt(0);
            }
        });
    }
    private void ExecuteStopThread(object? parameter)
    {
        IsAutoAdding = false;
        OnPropertyChanged(nameof(CanManualAdd));
        RaiseCanExecuteChanged();
    }
    private bool CanExecuteStopThread(object? parameter)
    {
        return IsAutoAdding;
    }
    private void ExecuteResumeThread(object? parameter)
    {
        IsAutoAdding = true;
        OnPropertyChanged(nameof(CanManualAdd));
        RaiseCanExecuteChanged();
    }
    private bool CanExecuteResumeThread(object? parameter)
    {
        return !IsAutoAdding;
    }
    private void ExecuteManualAdd(object? parameter)
    {
        if (!CanExecuteManualAdd(parameter) || string.IsNullOrWhiteSpace(ManualEntryText))
            return;
        _dataCounter++;
        var newItem = new DataItem
        {
            Id = _dataCounter,
            OperationType = "Manual Entry",
            Content = ManualEntryText.Trim(),
            Timestamp = DateTime.Now
        };
        Application.Current.Dispatcher.Invoke(() =>
        {
            DataItems.Add(newItem);
            LatestData = newItem.FullDescription;
            MaintainDataLimit();
        });
        ManualEntryText = string.Empty;
    }
    private bool CanExecuteManualAdd(object? parameter)
    {
        return CanManualAdd && !string.IsNullOrWhiteSpace(ManualEntryText);
    }
    #endregion
    #region COM Events 
    private void OnConnectionStatusChanged(object? sender, bool isConnected)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            IsConnected = isConnected;
            ConnectionStatus = isConnected ? $"Connected to {SelectedPort}" : "Disconnected";
            RaiseCanExecuteChanged();
        });
    }
    private void OnDataReceived(object? sender, string data)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            AddComDataItem("Read", data, true);
        });
    }
    private void OnDataWritten(object? sender, string data)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            AddComDataItem("Write", data, true);
        });
    }
    #endregion
    #region COM Operations
    private async Task ExecuteRefreshPortsAsync()
    {
        try
        {
            var ports = await _comPortService.GetAvailablePortsAsync();

            Application.Current.Dispatcher.Invoke(() =>
            {
                AvailablePorts.Clear();
                foreach (var port in ports)
                {
                    AvailablePorts.Add(port);
                }
                if (AvailablePorts.Count > 0 && !AvailablePorts.Contains(SelectedPort))
                {
                    SelectedPort = AvailablePorts[0];
                }
            });
            await _logService.LogInfoAsync($"Refreshed ports: {string.Join(", ", ports)}", "MainPageViewModel");
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync("Failed to refresh COM ports", ex, "MainPageViewModel");
        }
    }
    private async Task ExecuteConnectAsync()
    {
        try
        {
            var settings = new ComPortSettings
            {
                PortName = SelectedPort,
                BaudRate = BaudRate,
                DataBits = 8,
                StopBits = 1,
                Parity = "None",
                ReadTimeout = 5000,
                WriteTimeout = 5000
            };
            var connected = await _comPortService.ConnectAsync(settings);
            if (connected)
            {
                AddComDataItem("Connect", $"Connected to {SelectedPort} at {BaudRate} baud", true);
                await _logService.LogInfoAsync($"Connected with continuous reading enabled", "MainPageViewModel");
            }
            else
            {
                AddComDataItem("Connect", $"Failed to connect to {SelectedPort}", false);
            }
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync($"Connection error: {ex.Message}", ex, "MainPageViewModel");
            AddComDataItem("Connect", $"Error: {ex.Message}", false);
        }
    }
    private async Task ExecuteDisconnectAsync()
    {
        try
        {
            await _comPortService.DisconnectAsync();
            AddComDataItem("Disconnect", $"Disconnected from {SelectedPort}", true);
            await _logService.LogInfoAsync($"Disconnected and stopped continuous reading", "MainPageViewModel");
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync($"Disconnect error: {ex.Message}", ex, "MainPageViewModel");
            AddComDataItem("Disconnect", $"Error: {ex.Message}", false);
        }
    }
    private async Task ExecuteSendCommandAsync()
    {
        try
        {
            var command = CommandText?.Trim();
            
            if (string.IsNullOrWhiteSpace(command))
            {
                AddComDataItem("Command", "No command specified", false);
                return;
            }
            
            await _logService.LogInfoAsync($"Initiating synchronized command: {command}", "MainPageViewModel");
            AddComDataItem("SyncWrite", command, true);
            
            var response = await _comPortService.SendCommandAsync(command);
            
            // Обработка специальных команд
            var displayResponse = response;
            if (command.StartsWith("ENERGY?"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(response, @"(\d+\.?\d*)\s*(mJ|J|uJ)");
                if (match.Success && double.TryParse(match.Groups[1].Value, out var energy))
                {
                    var unit = match.Groups[2].Value.ToLower();
                    energy = unit switch
                    {
                        "j" => energy * 1000,
                        "uj" => energy / 1000,
                        _ => energy
                    };
                    displayResponse = $"{energy:F3} mJ";
                }
            }
            
            AddComDataItem("SyncRead", displayResponse, true);
            CommandText = string.Empty;
            await _logService.LogInfoAsync($"Synchronized command completed. Command: {command}, Response: {response}", "MainPageViewModel");
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync($"Synchronized command error: {ex.Message}", ex, "MainPageViewModel");
            AddComDataItem("SyncCommand", $"Error: {ex.Message}", false);
        }
    }
    private async Task ExecuteSetWavelengthAsync()
    {
        try
        {
            // Валидация длины волны
            if (Wavelength < 190 || Wavelength > 1100)
            {
                AddComDataItem("SetWavelength", $"Invalid wavelength: {Wavelength:F1} nm (must be 190-1100 nm)", false);
                return;
            }
            
            var command = $"WAVELENGTH {Wavelength:F1}";
            await _logService.LogInfoAsync($"Setting wavelength to {Wavelength:F1} nm", "MainPageViewModel");
            
            var response = await _comPortService.SendCommandAsync(command);
            
            // Проверяем успешность установки
            if (response.ToUpper().Contains("OK") || response.ToUpper().Contains("SUCCESS"))
            {
                AddComDataItem("SetWavelength", $"Successfully set to {Wavelength:F1} nm", true);
                await _logService.LogInfoAsync($"Wavelength set successfully: {Wavelength:F1} nm", "MainPageViewModel");
            }
            else
            {
                AddComDataItem("SetWavelength", $"{Wavelength:F1} nm → {response}", true);
                await _logService.LogInfoAsync($"Wavelength set response: {response}", "MainPageViewModel");
            }
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync($"Set wavelength error: {ex.Message}", ex, "MainPageViewModel");
            AddComDataItem("SetWavelength", $"Error: {ex.Message}", false);
        }
    }
    private async Task ExecuteGetEnergyAsync()
    {
        try
        {
            await _logService.LogInfoAsync("Requesting energy measurement", "MainPageViewModel");
            
            var response = await _comPortService.SendCommandAsync("ENERGY?");
            
            // Улучшенный парсинг энергии
            var match = System.Text.RegularExpressions.Regex.Match(response, @"(\d+\.?\d*)\s*(mJ|J|uJ)");
            double energy = 0;
            string unit = "mJ";
            
            if (match.Success && double.TryParse(match.Groups[1].Value, out energy))
            {
                unit = match.Groups[2].Value.ToLower();
                energy = unit switch
                {
                    "j" => energy * 1000,    // J → mJ
                    "uj" => energy / 1000,   // µJ → mJ
                    "mj" => energy,          // mJ → mJ
                    _ => energy 
                };
                
                AddComDataItem("GetEnergy", $"{energy:F3} mJ", true);
                await _logService.LogInfoAsync($"Energy measurement: {energy:F3} mJ", "MainPageViewModel");
            }
            else
            {
                // Если не удалось распарсить, показываем исходный ответ
                AddComDataItem("GetEnergy", $"Raw response: {response}", true);
                await _logService.LogInfoAsync($"Energy measurement raw response: {response}", "MainPageViewModel");
            }
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync($"Get energy error: {ex.Message}", ex, "MainPageViewModel");
            AddComDataItem("GetEnergy", $"Error: {ex.Message}", false);
        }
    }
    private async Task ExecuteResetDeviceAsync()
    {
        try
        {
            var response = await _comPortService.SendCommandAsync("*RST");
            AddComDataItem("Reset", response, true);
            await _logService.LogInfoAsync($"Synchronized device reset completed", "MainPageViewModel");
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync($"Reset device error: {ex.Message}", ex, "MainPageViewModel");
            AddComDataItem("Reset", $"Error: {ex.Message}", false);
        }
    }
    #endregion
    #region Helpers
    private void AddComDataItem(string operationType, string content, bool success)
    {
        var newItem = new DataItem
        {
            Id = ++_dataCounter,
            OperationType = success ? operationType : $"{operationType} (Failed)",
            Content = content,
            Timestamp = DateTime.Now
        };
        Application.Current.Dispatcher.Invoke(() =>
        {
            DataItems.Add(newItem);
            LatestData = newItem.FullDescription;
            MaintainDataLimit();
        });
    }
    private void MaintainDataLimit()
    {
        if (DataItems.Count > 50)
        {
            SaveOldDataToFile();
            for (int i = DataItems.Count - 1; i >= 50; i--)
            {
                DataItems.RemoveAt(0);
            }
        }
    }
    private void SaveOldDataToFile()
    {
        try
        {
            var fileName = $"OperationHistory_{DateTime.Now:yyyyMMdd}.csv";
            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ProjectLogs", fileName);       
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            bool fileExists = File.Exists(filePath);
            using (var writer = new StreamWriter(filePath, append: true))
            {
                if (!fileExists)
                {
                    writer.WriteLine("ID,OperationType,Content,Timestamp,Wavelength,Energy");
                }
                var itemsToRemove = DataItems.Take(DataItems.Count - 25).ToList();
                foreach (var item in itemsToRemove)
                {
                    var wavelength = item.Wavelength?.ToString("F1") ?? "";
                    var energy = item.Energy?.ToString("F3") ?? "";
                    writer.WriteLine($"{item.Id},{item.OperationType},\"{item.Content}\",{item.Timestamp:yyyy-MM-dd HH:mm:ss},{wavelength},{energy}");
                }
            }
            _logService.LogInfoAsync($"Saved {DataItems.Count - 50} old records to {filePath}", "MainPageViewModel");
        }
        catch (Exception ex)
        {
            _logService.LogErrorAsync("Failed to save old data to file", ex, "MainPageViewModel");
        }
    }
    #endregion
    #region Commands
        private void ExecuteLogout(object? parameter)
    {
        if (IsConnected)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _comPortService.DisconnectAsync();
                }
                catch (Exception ex)
                {
                    await _logService.LogErrorAsync("Error during logout disconnect", ex, "MainPageViewModel");
                }
            });
        }
            _navigationService.NavigateToLogin();
    }
    private void RaiseCanExecuteChanged()
    {
        if (ConnectCommand is RelayCommand connectCmd) connectCmd.RaiseCanExecuteChanged();
        if (DisconnectCommand is RelayCommand disconnectCmd) disconnectCmd.RaiseCanExecuteChanged();
        if (SendCommandCommand is RelayCommand sendCmd) sendCmd.RaiseCanExecuteChanged();
        if (SetWavelengthCommand is RelayCommand waveCmd) waveCmd.RaiseCanExecuteChanged();
        if (GetEnergyCommand is RelayCommand energyCmd) energyCmd.RaiseCanExecuteChanged();
        if (ResetDeviceCommand is RelayCommand resetCmd) resetCmd.RaiseCanExecuteChanged();
        if (StopThreadCommand is RelayCommand stopCmd) stopCmd.RaiseCanExecuteChanged();
        if (ResumeThreadCommand is RelayCommand resumeCmd) resumeCmd.RaiseCanExecuteChanged();
        if (ManualAddCommand is RelayCommand manualCmd) manualCmd.RaiseCanExecuteChanged();
        if (MeasureMultipleCommand is RelayCommand measureCmd) measureCmd.RaiseCanExecuteChanged();
        if (CancelMeasurementCommand is RelayCommand cancelCmd) cancelCmd.RaiseCanExecuteChanged();
    }
    #endregion
    #region Multi-Wavelength Measurements
    private async Task ExecuteMeasureMultipleAsync()
    {
        try
        {
            var wavelengths = ParseWavelengths(WavelengthsInput);
            if (wavelengths.Count == 0)
            {
                MeasurementStatus = "No valid wavelengths found. Please enter wavelengths in range 190-1100 nm.";
                await _logService.LogWarningAsync("No valid wavelengths provided for measurement", "MainPageViewModel");
                return;
            }
            IsMeasuring = true;
            MeasurementProgress = 0.0;
            MeasurementStatus = $"Starting measurements for {wavelengths.Count} wavelengths...";
            _measurementCancellationTokenSource = new CancellationTokenSource();
            ClearChartData();
            await _logService.LogInfoAsync($"Starting multi-wavelength measurement session with {wavelengths.Count} wavelengths", "MainPageViewModel");
            for (int i = 0; i < wavelengths.Count; i++)
            {
                if (_measurementCancellationTokenSource.Token.IsCancellationRequested)
                {
                    MeasurementStatus = "Measurements cancelled by user";
                    await _logService.LogInfoAsync("Multi-wavelength measurement cancelled by user", "MainPageViewModel");
                    break;
                }
                var wavelength = wavelengths[i];
                var measurementId = ++_measurementCounter;
                try
                {
                    MeasurementProgress = (double)i / wavelengths.Count * 100;
                    MeasurementStatus = $"Measuring wavelength {i + 1}/{wavelengths.Count}: {wavelength:F1} nm";
                    await _logService.LogInfoAsync($"Starting measurement {measurementId} for wavelength {wavelength:F1} nm", "MainPageViewModel");
                    var setWavelengthResponse = await _comPortService.SendCommandAsync($"WAVELENGTH {wavelength:F1}");
                    await _logService.LogDebugAsync($"Wavelength set response: {setWavelengthResponse}", "MainPageViewModel");
                    await Task.Delay(500, _measurementCancellationTokenSource.Token);
                    var energyResponse = await _comPortService.SendCommandAsync("ENERGY?");
                    var match = System.Text.RegularExpressions.Regex.Match(energyResponse, @"(\d+\.?\d*)\s*(mJ|J|uJ)");
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
                    var result = new MeasurementResult
                    {
                        Id = measurementId,
                        Wavelength = wavelength,
                        Energy = energy,
                        Timestamp = DateTime.Now,
                        Status = "Success",
                        Notes = $"Response: {energyResponse}"
                    };
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MeasurementResults.Add(result);
                    });
                    AddChartPoint(wavelength, energy);
                    AddComDataItem("MultiMeasurement", $"λ={wavelength:F1}nm, E={energy:F3}mJ", true);
                    await _logService.LogInfoAsync($"Measurement {measurementId} completed successfully: λ={wavelength:F1}nm, E={energy:F3}mJ", "MainPageViewModel");
                }
                catch (OperationCanceledException)
                {
                    var cancelResult = new MeasurementResult
                    {
                        Id = measurementId,
                        Wavelength = wavelength,
                        Energy = 0,
                        Timestamp = DateTime.Now,
                        Status = "Cancelled",
                        Notes = "Measurement cancelled by user"
                    };
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MeasurementResults.Add(cancelResult);
                    });
                    await _logService.LogInfoAsync($"Measurement {measurementId} cancelled for wavelength {wavelength:F1} nm", "MainPageViewModel");
                    break;
                }
                catch (Exception ex)
                {
                    var errorResult = new MeasurementResult
                    {
                        Id = measurementId,
                        Wavelength = wavelength,
                        Energy = 0,
                        Timestamp = DateTime.Now,
                        Status = "Error",
                        Notes = $"Error: {ex.Message}"
                    };
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MeasurementResults.Add(errorResult);
                    });
                    AddComDataItem("MultiMeasurement", $"λ={wavelength:F1}nm, Error: {ex.Message}", false);
                    await _logService.LogErrorAsync($"Measurement {measurementId} failed for wavelength {wavelength:F1} nm", ex, "MainPageViewModel");
                }
            }
            MeasurementProgress = 100.0;
            var successCount = MeasurementResults.Count(r => r.Status == "Success");
            var errorCount = MeasurementResults.Count(r => r.Status == "Error");
            var cancelledCount = MeasurementResults.Count(r => r.Status == "Cancelled");
            MeasurementStatus = $"Measurements completed: {successCount} successful, {errorCount} errors, {cancelledCount} cancelled";
            await _logService.LogInfoAsync($"Multi-wavelength measurement session completed: {successCount} successful, {errorCount} errors, {cancelledCount} cancelled", "MainPageViewModel");
        }
        catch (Exception ex)
        {
            MeasurementStatus = $"Measurement session failed: {ex.Message}";
            await _logService.LogErrorAsync("Multi-wavelength measurement session failed", ex, "MainPageViewModel");
        }
        finally
        {
            IsMeasuring = false;
            _measurementCancellationTokenSource?.Dispose();
            _measurementCancellationTokenSource = null;
        }
    }
    private void ExecuteCancelMeasurement(object? parameter)
    {
        try
        {
            _measurementCancellationTokenSource?.Cancel();
            MeasurementStatus = "Cancelling measurements...";
            _logService.LogInfoAsync("User requested measurement cancellation", "MainPageViewModel");
        }
        catch (Exception ex)
        {
            _logService.LogErrorAsync("Error during measurement cancellation", ex, "MainPageViewModel");
        }
    }
    private List<double> ParseWavelengths(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return new List<double>();
        try
        {
            var wavelengths = input
                .Split(new char[] { ',', ';', ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .Select(s => double.TryParse(s, out var wl) ? wl : (double?)null)
                .Where(wl => wl.HasValue && wl >= 190 && wl <= 1100)  
                .Select(wl => wl!.Value)
                .Distinct()  
                .OrderBy(wl => wl)  
                .ToList();
            _logService.LogDebugAsync($"Parsed {wavelengths.Count} valid wavelengths from input: {string.Join(", ", wavelengths.Select(w => $"{w:F1}"))}nm", "MainPageViewModel");
            return wavelengths;
        }
        catch (Exception ex)
        {
            _logService.LogErrorAsync("Error parsing wavelengths input", ex, "MainPageViewModel");
            return new List<double>();
        }
    }
    #endregion
    #region Chart Management
    private void InitializeChart()
    {
        try
        {
            _energyChartSeries = new ISeries[]
            {
                new LineSeries<ObservablePoint>
                {
                    Values = _chartDataPoints,
                    Name = "Energy vs Wavelength",
                    GeometrySize = 8,
                    GeometryStroke = new SolidColorPaint(SKColors.CornflowerBlue) { StrokeThickness = 2 },
                    GeometryFill = new SolidColorPaint(SKColors.CornflowerBlue),
                    Stroke = new SolidColorPaint(SKColors.CornflowerBlue) { StrokeThickness = 3 },
                    Fill = null, 
                    LineSmoothness = 0.3f 
                }
            };
            OnPropertyChanged(nameof(EnergyChartSeries));
            _logService.LogDebugAsync("Chart initialized successfully", "MainPageViewModel");
        }
        catch (Exception ex)
        {
            _logService.LogErrorAsync("Error initializing chart", ex, "MainPageViewModel");
        }
    }
    private void AddChartPoint(double wavelength, double energy)
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var point = new ObservablePoint(wavelength, energy);
                _chartDataPoints.Add(point);
                _logService.LogDebugAsync($"Added chart point: λ={wavelength:F1}nm, E={energy:F3}mJ", "MainPageViewModel");
            });
        }
        catch (Exception ex)
        {
            _logService.LogErrorAsync($"Error adding chart point (λ={wavelength:F1}nm, E={energy:F3}mJ)", ex, "MainPageViewModel");
        }
    }
    private void ClearChartData()
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _chartDataPoints.Clear();
                _logService.LogDebugAsync("Chart data cleared", "MainPageViewModel");
            });
        }
        catch (Exception ex)
        {
            _logService.LogErrorAsync("Error clearing chart data", ex, "MainPageViewModel");
        }
    }
    #endregion
} 