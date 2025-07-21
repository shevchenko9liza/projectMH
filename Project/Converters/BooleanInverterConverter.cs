using System;
using System.Globalization;
using System.Windows.Data;
namespace Project.Converters
{
    public class BooleanInverterConverter : IValueConverter
    {
        public static BooleanInverterConverter Instance { get; } = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool booleanValue)
            {
                return !booleanValue;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool booleanValue)
            {
                return !booleanValue;
            }
            return false;
        }
    }
} 