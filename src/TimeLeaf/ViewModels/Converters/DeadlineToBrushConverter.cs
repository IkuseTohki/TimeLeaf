using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace TimeLeaf.ViewModels.Converters;

/// <summary>
/// 期限（DateTime?）を、緊急度に応じた色（Brush）に変換するコンバーター。
/// </summary>
public class DeadlineToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime deadline)
        {
            var today = DateTime.Today;
            var diff = (deadline.Date - today).TotalDays;

            if (diff < 0) return Brushes.Crimson; // 期限切れ
            if (diff == 0) return Brushes.OrangeRed; // 今日
            if (diff <= 3) return Brushes.Orange; // 直近
        }

        // デフォルトの色（リソースから取得するのが理想的だが、ここでは標準のテキスト色を想定）
        return Application.Current?.Resources["TextPrimaryBrush"] as Brush ?? Brushes.Black;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
