using System;
using System.Globalization;
using System.Windows.Data;
using TimeLeaf.Models.Enums;

namespace TimeLeaf.ViewModels.Converters;

/// <summary>
/// タスクの状態（TaskStatus）を絵文字アイコンに変換するコンバーター。
/// </summary>
public class TaskStatusToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TaskStatus status)
        {
            return status switch
            {
                TaskStatus.NotStarted => "⚪",
                TaskStatus.InProgress => "🔵",
                TaskStatus.InReview => "🟡",
                TaskStatus.Completed => "🟢",
                _ => "❓"
            };
        }
        return "❓";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
