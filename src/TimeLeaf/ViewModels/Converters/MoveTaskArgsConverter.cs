using System;
using System.Globalization;
using System.Windows.Data;
using TimeLeaf.ViewModels.Workspace;

namespace TimeLeaf.ViewModels.Converters;

/// <summary>
/// タスク移動コマンドの引数 (MoveTaskArgs) を生成するためのマルチコンバーター。
/// </summary>
public class MoveTaskArgsConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2)
            return null!;

        var task = values[0] as ProjectTaskViewModel;
        var parentId = values[1] as Guid?;

        if (task == null)
            return null!;

        return new MoveTaskArgs(task, parentId);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
