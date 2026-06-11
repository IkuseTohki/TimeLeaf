using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using TimeLeaf.Models;
using TimeLeaf.Models.Entities;
using TimeLeaf.Services;
using TimeLeaf.UseCases;

namespace TimeLeaf.ViewModels.Workspace
{
    /// <summary>
    /// タイムライン画面の表示と操作を制御する ViewModel。
    /// </summary>
    public partial class ProjectTimelineViewModel : ObservableObject
    {
        private readonly ProjectViewModel _projectViewModel;
        private readonly GetTimelineRowsUseCase _getTimelineRowsUseCase;

        [ObservableProperty]
        private TimelineViewMode _currentViewMode = TimelineViewMode.Dual;

        [ObservableProperty]
        private ObservableCollection<TimelineRowModel> _rows = new();

        [ObservableProperty]
        private ObservableCollection<DateTime> _timelineDates = new();

        [ObservableProperty]
        private double _totalWidth;

        [ObservableProperty]
        private DateTime _today = new DateTime(2026, 6, 11);

        [ObservableProperty]
        private DateTime _baseDate;

        [ObservableProperty]
        private double _scrollOffset;

        public ProjectTimelineViewModel(
            ProjectViewModel projectViewModel,
            GetTimelineRowsUseCase getTimelineRowsUseCase
        )
        {
            _projectViewModel = projectViewModel ?? throw new ArgumentNullException(nameof(projectViewModel));
            _getTimelineRowsUseCase =
                getTimelineRowsUseCase ?? throw new ArgumentNullException(nameof(getTimelineRowsUseCase));

            LoadTimelineData();
        }

        /// <summary>
        /// 現在のプロジェクトデータに基づき、タイムライン表示データをリロードします。
        /// </summary>
        public void LoadTimelineData()
        {
            var project = _projectViewModel.Model;
            var workItems = project.Tasks.Cast<ProjectWorkItem>().Concat(project.Containers.Cast<ProjectWorkItem>());

            var timelineRows = _getTimelineRowsUseCase.Execute(workItems, Today).ToList();

            Rows.Clear();
            foreach (var row in timelineRows)
            {
                Rows.Add(row);
            }

            // 表示範囲の計算
            var allDates = Rows.SelectMany(r => new[] { r.PlannedStart, r.PlannedEnd, r.ActualStart, r.ActualEnd })
                .Where(d => d.HasValue)
                .Select(d => d.Value)
                .ToList();

            DateTime startDate;
            DateTime endDate;

            if (allDates.Any())
            {
                startDate = allDates.Min().AddDays(-14); // スクロールバッファ含め少し広めに
                endDate = allDates.Max().AddDays(14);
            }
            else
            {
                startDate = Today.AddDays(-14);
                endDate = Today.AddDays(21);
            }

            // 週の開始（月曜日）に調整
            startDate = startDate.AddDays(
                -(int)(startDate.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)startDate.DayOfWeek - 1)
            );
            BaseDate = startDate;

            TimelineDates.Clear();
            var current = startDate;
            while (current <= endDate)
            {
                TimelineDates.Add(current);
                current = current.AddDays(1);
            }

            TotalWidth = TimelineDates.Count * 60.0;

            // スクロール位置の計算 (今日 - 3日)
            var scrollDate = Today.AddDays(-3);
            if (scrollDate < BaseDate)
                scrollDate = BaseDate;
            ScrollOffset = (scrollDate.Date - BaseDate.Date).TotalDays * 60.0;
        }
    }
}
