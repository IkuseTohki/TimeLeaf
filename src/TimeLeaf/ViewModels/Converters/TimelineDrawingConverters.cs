using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using TimeLeaf.Models;
using TimeLeaf.Services;

namespace TimeLeaf.ViewModels.Converters
{
    // 日付を X 座標に変換
    public class DateToXConverter : IMultiValueConverter
    {
        private const double DayWidth = 60.0;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is DateTime date && values[1] is DateTime baseDate)
            {
                var offset = double.TryParse(parameter?.ToString(), out var result) ? result : 0.0;
                return (date.Date - baseDate.Date).TotalDays * DayWidth + offset;
            }
            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }

    // 期間を幅に変換
    public class DateRangeToWidthConverter : IValueConverter
    {
        private const double DayWidth = 60.0;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimelineRowModel row)
            {
                DateTime? start = parameter?.ToString() == "Planned" ? row.PlannedStart : row.ActualStart;
                DateTime? end = parameter?.ToString() == "Planned" ? row.PlannedEnd : row.ActualEnd;

                if (start.HasValue && end.HasValue)
                {
                    return Math.Max(10.0, (end.Value.Date - start.Value.Date).TotalDays * DayWidth + DayWidth);
                }
            }
            return 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }

    // 進捗率を幅に変換 (親要素の幅に対して)
    public class PercentageToWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is int progress && values[1] is double totalWidth)
            {
                return totalWidth * (progress / 100.0);
            }
            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }

    // 全行データから稲妻線のジオメトリを生成
    public class RowsToLightningPathConverter : IMultiValueConverter
    {
        private const double DayWidth = 60.0;
        private const double RowHeight = 60.0;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (
                values.Length >= 3
                && values[0] is ObservableCollection<TimelineRowModel> rows
                && values[1] is DateTime baseDate
                && values[2] is DateTime today
                && rows.Any()
            )
            {
                var todayX = (today.Date - baseDate.Date).TotalDays * DayWidth + (DayWidth / 2);
                var figures = new PathFigureCollection();
                var figure = new PathFigure { StartPoint = new Point(todayX, 0) };
                var segments = new PathSegmentCollection();

                for (int i = 0; i < rows.Count; i++)
                {
                    var row = rows[i];
                    var centerY = i * RowHeight + (RowHeight / 2);
                    var offsetX = row.ProgressOffsetDays * DayWidth;

                    segments.Add(new LineSegment(new Point(todayX + offsetX, centerY), true));
                }

                segments.Add(new LineSegment(new Point(todayX, rows.Count * RowHeight), true));
                figure.Segments = segments;
                figures.Add(figure);

                return new PathGeometry(figures);
            }
            return Geometry.Empty;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }

    public class DateToHeaderFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime date && parameter is string format)
            {
                if (format == "DayOfWeek")
                    return date.ToString("ddd", culture);
                if (format == "Day")
                {
                    return date.Day.ToString();
                }
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }

    // 日付ヘッダーのテキスト（1日ならM/d、それ以外ならd）を決定する
    public class DateToDayHeaderTextConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is DateTime date && values[1] is DateTime baseDate)
            {
                if (date.Date == baseDate.Date || date.Day == 1)
                {
                    return date.ToString("M/d", culture);
                }
                return date.Day.ToString();
            }
            return string.Empty;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }

    // 非稼働日（休日・祝日）かどうかを判定する
    public class DateToIsNonWorkdayConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is DateTime date && values[1] is WorkdayService workdayService)
            {
                // IsWorkday が false なら「非稼働」なので True を返す
                return !workdayService.IsWorkday(date);
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
