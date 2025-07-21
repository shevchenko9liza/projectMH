# Serial Port Communication Dashboard

A WPF application for communicating with devices via serial ports. Built with .NET 8 and following the MVVM pattern.

## What it does

This dashboard helps you connect to and control devices through COM ports. It's perfect for:
- Testing serial communication with hardware devices
- Sending commands to embedded systems
- Measuring energy levels at different wavelengths
- Logging all communication activities

## Features

### COM Port Connection
- Auto-detect available COM ports
- Configurable baud rates (9600, 19200, 38400, 57600, 115200)
- One-click connect/disconnect
- Real-time connection status

### Device Commands
- Pre-defined command library (CONFIG?, MEASURE?, STATUS, etc.)
- Custom command input
- Wavelength setting (190-1100 nm range)
- Energy measurement
- Device reset functionality

### Multi-Wavelength Measurements
- Batch measurements across multiple wavelengths
- Real-time status updates
- Comprehensive measurement logging

### Results and History
- Live measurement results display
- Operation history (last 50 entries)
- Automatic file logging for older entries
- Timestamp tracking

## Getting Started

### Prerequisites
- Windows 10/11
- .NET 8 Runtime
- A device with serial communication capability

### Installation
1. Download the latest release
2. Extract the files
3. Run `Project.exe`

### First Steps
1. Click "Refresh Ports" to see available COM ports
2. Select your device's COM port
3. Choose the correct baud rate
4. Click "Connect"
5. Start sending commands!

## Usage Tips

- **Port Selection**: If you don't see your device, try refreshing the ports list
- **Baud Rate**: Make sure it matches your device's settings
- **Commands**: Use the dropdown for common commands, or type custom ones
- **Wavelength**: Enter values between 190-1100 nm
- **Logs**: Check the operation history for troubleshooting

## Troubleshooting

**Can't connect?**
- Verify the COM port is correct
- Check if another application is using the port
- Ensure the baud rate matches your device

**Commands not working?**
- Make sure the device is connected
- Check the command syntax
- Look at the operation history for error messages

**No ports showing?**
- Ensure your device is properly connected
- Try refreshing the ports list
- Check Windows Device Manager

## Technical Details

- **Framework**: .NET 8 WPF
- **Architecture**: MVVM with Dependency Injection
- **Communication**: Asynchronous serial port handling
- **Logging**: Built-in logging service with file output
- **UI**: Modern dark theme with responsive design