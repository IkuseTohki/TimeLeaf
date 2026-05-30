using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows.Data;

namespace TimeLeaf.ViewModels.Converters;

/// <summary>
/// Enum の [Display] 属性から表示名（Name）を取得するコンバーター。
/// </summary>
public class EnumToDisplayNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
            return string.Empty;

        var enumType = value.GetType();
        if (!enumType.IsEnum)
            return value.ToString() ?? string.Empty;

        var name = value.ToString();
        if (name == null)
            return string.Empty;

        var field = enumType.GetField(name);
        if (field == null)
            return name;

        var displayAttribute = field.GetCustomAttribute<DisplayAttribute>();
        return displayAttribute?.Name ?? name;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
