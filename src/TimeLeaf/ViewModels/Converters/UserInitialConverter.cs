using System;
using System.Globalization;
using System.Windows.Data;

namespace TimeLeaf.ViewModels.Converters;

/// <summary>
/// 表示名から頭文字（イニシャル）を取得するコンバーター。
/// </summary>
public class UserInitialConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string name && !string.IsNullOrEmpty(name))
        {
            return name.Substring(0, 1).ToUpper();
        }
        return "?";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
