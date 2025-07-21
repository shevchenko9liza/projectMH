namespace Project.Core.Models;
public class ComPortSettings
{
    public string PortName { get; set; } = "COM1";
    public int BaudRate { get; set; } = 9600;
    public int DataBits { get; set; } = 8;
    public int StopBits { get; set; } = 1;
    public string Parity { get; set; } = "None";
    public int ReadTimeout { get; set; } = 5000;
    public int WriteTimeout { get; set; } = 5000;
    public bool IsConnected { get; set; } = false;
    public string DisplayName => $"{PortName} ({BaudRate} baud)";
} 