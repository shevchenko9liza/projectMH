using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
namespace Project.ViewModels;
public abstract class BaseViewModel : INotifyPropertyChanged, INotifyDataErrorInfo
{
    private readonly Dictionary<string, List<string>> _errors = new();
    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;   
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
    public bool HasErrors => _errors.Count > 0;
    public IEnumerable GetErrors(string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            return _errors.Values.SelectMany(x => x);
        }
        return _errors.TryGetValue(propertyName, out var errors) ? errors : Enumerable.Empty<string>();
    }
    protected void AddError(string propertyName, string error)
    {
        if (!_errors.TryGetValue(propertyName, out var errors))
        {
            errors = new List<string>();
            _errors[propertyName] = errors;
        }
        if (!errors.Contains(error))
        {
            errors.Add(error);
            OnErrorsChanged(propertyName);
        }
    }
    protected void RemoveError(string propertyName, string error)
    {
        if (_errors.TryGetValue(propertyName, out var errors))
        {
            errors.Remove(error);
            if (errors.Count == 0)
            {
                _errors.Remove(propertyName);
            }
            OnErrorsChanged(propertyName);
        }
    }
    protected void ClearErrors(string propertyName)
    {
        if (_errors.Remove(propertyName))
        {
            OnErrorsChanged(propertyName);
        }
    }
    private void OnErrorsChanged(string propertyName)
    {
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        OnPropertyChanged(nameof(HasErrors));
    }
    protected void ValidateWavelength(double wavelength, [CallerMemberName] string? propertyName = null)
    {
        if (propertyName == null) return;        
        ClearErrors(propertyName);        
        if (wavelength < 190)
        {
            AddError(propertyName, "Wavelength must be at least 190 nm");
        }
        else if (wavelength > 1100)
        {
            AddError(propertyName, "Wavelength must not exceed 1100 nm");
        }
    }
    protected void ValidateWavelengthInput(string input, [CallerMemberName] string? propertyName = null)
    {
        if (propertyName == null) return;        
        ClearErrors(propertyName);        
        if (string.IsNullOrWhiteSpace(input))
        {
            AddError(propertyName, "Wavelength input is required");
            return;
        }
        var wavelengths = input
            .Split(new char[] { ',', ';', ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();
        if (wavelengths.Count == 0)
        {
            AddError(propertyName, "No valid wavelengths found");
            return;
        }
        foreach (var wlStr in wavelengths)
        {
            if (!double.TryParse(wlStr, out var wl))
            {
                AddError(propertyName, $"'{wlStr}' is not a valid number");
                continue;
            }          
            if (wl < 190 || wl > 1100)
            {
                AddError(propertyName, $"Wavelength {wl:F1} nm is out of range (190-1100 nm)");
            }
        }
    }
    protected void ValidateCommand(string command, [CallerMemberName] string? propertyName = null)
    {
        if (propertyName == null) return;   
        ClearErrors(propertyName);
        if (string.IsNullOrWhiteSpace(command))
        {
            AddError(propertyName, "Command is required");
        }
        else if (command.Length > 50)
        {
            AddError(propertyName, "Command must be 50 characters or less");
        }
    }
    protected void ValidateManualEntry(string text, [CallerMemberName] string? propertyName = null)
    {
        if (propertyName == null) return;   
        ClearErrors(propertyName);
        if (string.IsNullOrWhiteSpace(text))
        {
            AddError(propertyName, "Manual entry text is required");
        }
        else if (text.Length > 100)
        {
            AddError(propertyName, "Manual entry must be 100 characters or less");
        }
    }
} 