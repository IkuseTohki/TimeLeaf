using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TimeLeaf.ViewModels.Converters;

/// <summary>
/// 文字列が空でない場合に Visibility.Visible を、空の場合に Visibility.Collapsed を返すコンバーター。
/// </summary>
public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
