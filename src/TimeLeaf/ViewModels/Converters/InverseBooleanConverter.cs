using System;
using System.Globalization;
using System.Windows.Data;

namespace TimeLeaf.ViewModels.Converters
{
    /// <summary>
    /// Boolean 値を反転させるコンバータ。
    /// </summary>
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }
    }
}
