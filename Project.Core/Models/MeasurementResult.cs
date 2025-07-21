namespace Project.Core.Models;
public class MeasurementResult
{
    public int Id { get; set; }
    public double Wavelength { get; set; }
    public double Energy { get; set; }
    public DateTime Timestamp { get; set; }
    public string Status { get; set; } = "Success";
    public string Notes { get; set; } = string.Empty;
    public string WavelengthDisplay => $"{Wavelength:F1} nm";
    public string EnergyDisplay => $"{Energy:F3} mJ";
    public string TimeDisplay => Timestamp.ToString("HH:mm:ss");
    public string FullDescription => $"λ={WavelengthDisplay}, E={EnergyDisplay}, Status={Status}, Time={TimeDisplay}";
} 