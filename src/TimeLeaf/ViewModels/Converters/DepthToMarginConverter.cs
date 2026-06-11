using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TimeLeaf.ViewModels.Converters
{
    public class DepthToMarginConverter : IValueConverter
    {
        public double Multiplier { get; set; } = 20.0;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int depth)
            {
                return new Thickness(depth * Multiplier + 16, 0, 0, 0);
            }
            return new Thickness(16, 0, 0, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
