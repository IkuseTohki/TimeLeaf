using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace TimeLeaf.ViewModels.Converters;

/// <summary>
/// 期限（DateTime?）を、緊急度に応じた状態名（string）に変換するコンバーター。
/// </summary>
public class DeadlineToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime deadline)
        {
            var today = DateTime.Today;
            var diff = (deadline.Date - today).TotalDays;

            if (diff < 0)
                return "Overdue";
            if (diff == 0)
                return "Today";
            if (diff <= 3)
                return "Near";

            return "Future";
        }

        return "None";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
