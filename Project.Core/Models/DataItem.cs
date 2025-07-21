namespace Project.Core.Models;
public class DataItem
{
    public int Id { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public double? Wavelength { get; set; }
    public double? Energy { get; set; }
    public string FormattedTime => Timestamp.ToString("HH:mm:ss.fff");
    public string Message => Content;
    public string FullDescription => $"{OperationType}: {Content}, Time: {FormattedTime}";
    public string WavelengthDisplay => Wavelength?.ToString("F1") + " nm" ?? "";
    public string EnergyDisplay => Energy?.ToString("F3") + " J" ?? "";
} 