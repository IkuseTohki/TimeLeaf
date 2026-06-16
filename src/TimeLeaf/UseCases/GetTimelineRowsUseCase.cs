using System;
using System.Collections.Generic;
using System.Linq;
using TimeLeaf.Models;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Enums;
using TimeLeaf.Models.Interfaces;
using TimeLeaf.Services;

namespace TimeLeaf.UseCases
{
    /// <summary>
    /// プロジェクトのタスク情報をタイムライン表示用のモデルに変換するユースケース。
    /// </summary>
    public class GetTimelineRowsUseCase
    {
        private readonly WorkdayService _workdayService;
        private readonly IUserService _userService;

        public GetTimelineRowsUseCase(WorkdayService workdayService, IUserService userService)
        {
            _workdayService = workdayService ?? throw new ArgumentNullException(nameof(workdayService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        /// <summary>
        /// 指定されたコンテナ内のワークアイテムをタイムライン表示用モデルのリストに変換します。
        /// </summary>
        /// <param name="container">対象のワークアイテムを保持するルートコンテナ。</param>
        /// <param name="today">基準日（今日）。稲妻線の計算に使用。省略時は現在日時。</param>
        /// <returns>タイムライン行モデルのリスト。</returns>
        public IEnumerable<TimelineRowModel> Execute(IWorkItemContainer container, DateTime? today = null)
        {
            var referenceDate = today ?? DateTime.Today;
            var results = new List<TimelineRowModel>();

            if (container == null)
                return results;

            foreach (var item in container.Children)
            {
                AppendItemRecursive(item, 0, results, referenceDate);
            }

            return results;
        }

        private void AppendItemRecursive(
            ProjectWorkItem item,
            int depth,
            List<TimelineRowModel> results,
            DateTime today
        )
        {
            var model = MapToRow(item, depth, today);
            results.Add(model);

            // 子アイテムを取得
            if (item is IWorkItemContainer container)
            {
                foreach (var child in container.Children)
                {
                    AppendItemRecursive(child, depth + 1, results, today);
                }
            }
        }

        private TimelineRowModel MapToRow(ProjectWorkItem item, int depth, DateTime today)
        {
            var model = new TimelineRowModel
            {
                TaskId = item.Id,
                Name = item.Name,
                Depth = depth,
                PlannedStart = item.PlannedStartDate,
                PlannedEnd = item.PlannedEndDate ?? item.Deadline,
                IsContainer = item is ProjectContainer,
            };

            if (item is ProjectTask task)
            {
                model.ActualStart = task.ActualStartDate;
                model.ActualEnd = task.ActualEndDate;

                // 進捗率の計算 (工数ベース)
                int progress = 0;
                if (task.Status == TaskStatus.Completed)
                {
                    progress = 100;
                }
                else if (task.EstimatedCost > 0)
                {
                    progress = (int)Math.Min(100, (task.ActualCost / task.EstimatedCost) * 100);
                }
                model.ProgressPercentage = progress;

                // ユーザー情報の取得
                if (task.Assignee.HasValue)
                {
                    var userName = _userService.GetUserName(task.Assignee.Value.ToString());
                    model.UserInitial = !string.IsNullOrEmpty(userName) ? userName[0].ToString().ToUpper() : "U";
                    model.UserColor = "#0984e3";
                }
                else
                {
                    model.UserInitial = string.Empty;
                    model.UserColor = "Transparent";
                }
            }
            else if (item is ProjectContainer container)
            {
                // コンテナの場合、もし計画日付が空なら子要素から集約する
                if (!model.PlannedStart.HasValue || !model.PlannedEnd.HasValue)
                {
                    var children = container.Children.ToList();
                    if (children.Any())
                    {
                        model.PlannedStart ??= children.Min(c => c.PlannedStartDate);
                        model.PlannedEnd ??= children.Max(c => c.PlannedEndDate ?? c.Deadline);
                    }
                }

                model.ActualStart = container.ActualStartDate;
                model.ActualEnd = container.ActualEndDate;
                model.ProgressPercentage = container.ProgressPercentage;
            }

            // 稲妻線用の進捗偏差計算
            if (model.PlannedStart.HasValue && model.PlannedEnd.HasValue)
            {
                model.ProgressOffsetDays = CalculateProgressOffset(model, today);
            }

            return model;
        }

        private double CalculateProgressOffset(TimelineRowModel row, DateTime today)
        {
            var start = row.PlannedStart!.Value;
            var end = row.PlannedEnd!.Value;

            if (today < start)
                return 0;

            // 予定期間の全稼働日数を算出
            var totalWorkdays = CountWorkdays(start, end);
            if (totalWorkdays <= 0)
                return 0;

            // 実績として「何稼働日分」終わっているか
            var actualDoneWorkdays = totalWorkdays * (row.ProgressPercentage / 100.0);

            // 今日までに「何稼働日」経過しているべきか
            var limitDate = today > end ? end : today;
            var expectedDoneWorkdays = CountWorkdays(start, limitDate);

            // 偏差 = 実績日数 - 予定上の今日までの日数
            return actualDoneWorkdays - expectedDoneWorkdays;
        }

        private int CountWorkdays(DateTime start, DateTime end)
        {
            var count = 0;
            var current = start.Date;
            while (current <= end.Date)
            {
                if (_workdayService.IsWorkday(current))
                    count++;
                current = current.AddDays(1);
            }
            return count;
        }
    }
}
