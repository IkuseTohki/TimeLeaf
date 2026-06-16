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
    public partial class ProjectTimelineViewModel : ObservableObject, IDisposable
    {
        private readonly ProjectViewModel _projectViewModel;
        private readonly GetTimelineRowsUseCase _getTimelineRowsUseCase;

        /// <summary>
        /// 稼働日計算サービス。
        /// XAMLからのバインディング（非稼働日の判定）に使用するため公開。
        /// </summary>
        public WorkdayService WorkdayService { get; }

        [ObservableProperty]
        private TimelineViewMode _currentViewMode = TimelineViewMode.Dual;

        [ObservableProperty]
        private ObservableCollection<TimelineRowModel> _rows = new();

        [ObservableProperty]
        private ObservableCollection<Milestone> _milestones = new();

        [ObservableProperty]
        private ObservableCollection<DateTime> _timelineDates = new();

        [ObservableProperty]
        private double _totalWidth;

        [ObservableProperty]
        private DateTime _today = DateTime.Today;

        [ObservableProperty]
        private DateTime _baseDate;

        [ObservableProperty]
        private double _scrollOffset;

        public ProjectTimelineViewModel(
            ProjectViewModel projectViewModel,
            GetTimelineRowsUseCase getTimelineRowsUseCase,
            WorkdayService workdayService
        )
        {
            _projectViewModel = projectViewModel ?? throw new ArgumentNullException(nameof(projectViewModel));
            _getTimelineRowsUseCase =
                getTimelineRowsUseCase ?? throw new ArgumentNullException(nameof(getTimelineRowsUseCase));
            WorkdayService = workdayService ?? throw new ArgumentNullException(nameof(workdayService));

            WorkdayService.CalendarChanged += OnCalendarChanged;

            LoadTimelineData();
        }

        private void OnCalendarChanged()
        {
            LoadTimelineData();
        }

        /// <summary>
        /// 現在のプロジェクトデータに基づき、タイムライン表示データをリロードします。
        /// </summary>
        public void LoadTimelineData()
        {
            var project = _projectViewModel.Model;
            var rootContainer = new ProjectRootContainer(project);

            var timelineRows = _getTimelineRowsUseCase.Execute(rootContainer, Today).ToList();

            Rows.Clear();
            foreach (var row in timelineRows)
            {
                Rows.Add(row);
            }

            Milestones.Clear();
            foreach (var m in project.Milestones.OrderBy(m => m.Date))
            {
                Milestones.Add(m);
            }

            // 表示範囲の計算
            var allDates = Rows.SelectMany(r => new[] { r.PlannedStart, r.PlannedEnd, r.ActualStart, r.ActualEnd })
                .OfType<DateTime>()
                .Concat(project.Milestones.Select(m => m.Date))
                .ToList();

            DateTime startDate;
            DateTime endDate;

            if (allDates.Count > 0)
            {
                startDate = allDates.Min().AddDays(-14);
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

            TotalWidth = TimelineDates.Count * TimelineLayoutConstants.DayWidth;

            // スクロール位置の計算 (今日 - 3日)
            var scrollDate = Today.AddDays(-3);
            if (scrollDate < BaseDate)
                scrollDate = BaseDate;
            ScrollOffset = (scrollDate.Date - BaseDate.Date).TotalDays * TimelineLayoutConstants.DayWidth;
        }

        public void Dispose()
        {
            WorkdayService.CalendarChanged -= OnCalendarChanged;
        }
    }
}
