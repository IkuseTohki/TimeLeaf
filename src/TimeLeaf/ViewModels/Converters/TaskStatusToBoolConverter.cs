using System;
using System.Globalization;
using System.Windows.Data;
using TimeLeaf.Models.Enums;

namespace TimeLeaf.ViewModels.Converters;

/// <summary>
/// タスクの状態が Completed かどうかをブール値に変換するコンバータ。
/// </summary>
public class TaskStatusToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is TimeLeaf.Models.Enums.TaskStatus status && status == TimeLeaf.Models.Enums.TaskStatus.Completed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (value is bool completed && completed)
            ? TimeLeaf.Models.Enums.TaskStatus.Completed
            : TimeLeaf.Models.Enums.TaskStatus.InProgress;
    }
}
