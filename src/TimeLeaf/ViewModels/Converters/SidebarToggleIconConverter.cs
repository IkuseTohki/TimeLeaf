using System;
using System.Globalization;
using System.Windows.Data;

namespace TimeLeaf.ViewModels.Converters;

/// <summary>
/// サイドバーの展開状態に応じてアイコン文字を切り替えるコンバータ。
/// </summary>
public class SidebarToggleIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (value is bool expanded && expanded) ? "❮" : "❯";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
