using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace TimeLeaf.ViewModels.Converters;

/// <summary>
/// 複数のバインディング値をオブジェクト配列としてそのまま返すマルチコンバーター。
/// コマンドへ複数の引数を渡す際に使用する。
/// </summary>
public class ArrayMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        // 参照を保持するためコピーを返す
        return values.ToArray();
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
