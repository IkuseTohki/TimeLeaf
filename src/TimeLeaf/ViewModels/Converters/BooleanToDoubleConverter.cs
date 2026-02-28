using System;
using System.Globalization;
using System.Windows.Data;

namespace TimeLeaf.ViewModels.Converters;

/// <summary>
/// ブール値を double 値（幅など）に変換するコンバータ。
/// パラメータ形式: "trueValue,falseValue"
/// </summary>
public class BooleanToDoubleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && parameter is string p)
        {
            var parts = p.Split(',');
            if (parts.Length == 2)
            {
                double trueValue = double.Parse(parts[0]);
                double falseValue = double.Parse(parts[1]);
                return b ? trueValue : falseValue;
            }
        }
        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
