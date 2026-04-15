using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using TimeLeaf.Models.Enums;

namespace TimeLeaf.ViewModels.Converters;

/// <summary>
/// 優先度が High (緊急) の場合のみ表示するコンバータ。
/// </summary>
public class PriorityToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is TaskPriority priority && priority == TaskPriority.High
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
