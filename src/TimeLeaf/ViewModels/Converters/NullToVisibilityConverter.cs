using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TimeLeaf.ViewModels.Converters;

/// <summary>
/// オブジェクトが null の場合に Visibility.Collapsed を、それ以外の場合に Visibility.Visible を返すコンバーター。
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isNull = value == null;

        // パラメータに "Invert" が指定されている場合は挙動を反転
        if (parameter?.ToString() == "Invert")
        {
            return isNull ? Visibility.Visible : Visibility.Collapsed;
        }

        return isNull ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
