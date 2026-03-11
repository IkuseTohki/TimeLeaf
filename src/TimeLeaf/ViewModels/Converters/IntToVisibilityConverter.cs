using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TimeLeaf.ViewModels.Converters;

/// <summary>
/// 整数値を Visibility に変換するコンバータ。
/// 0 の場合は Collapsed、1 以上の場合は Visible を返します。
/// ConverterParameter に 'Inverse' を指定すると、0 の場合に Visible、1 以上の場合は Collapsed を返します。
/// </summary>
public class IntToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isInverse = parameter?.ToString() == "Inverse";
        int count = 0;

        if (value is int intValue)
        {
            count = intValue;
        }

        bool isVisible = count > 0;
        if (isInverse)
        {
            isVisible = !isVisible;
        }

        return isVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
