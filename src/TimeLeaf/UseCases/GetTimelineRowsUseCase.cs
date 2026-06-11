using System;
using System.Collections.Generic;
using System.Linq;
using TimeLeaf.Models;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Enums;
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
        /// 指定されたワークアイテムリストをタイムライン表示用モデルのリストに変換します。
        /// </summary>
        /// <param name="items">対象のワークアイテム（タスクおよびコンテナ）リスト。</param>
        /// <param name="today">基準日（今日）。稲妻線の計算に使用。省略時は現在日時。</param>
        /// <returns>タイムライン行モデルのリスト。</returns>
        public IEnumerable<TimelineRowModel> Execute(IEnumerable<ProjectWorkItem> items, DateTime? today = null)
        {
            var referenceDate = today ?? DateTime.Today;
            var itemList = items?.ToList() ?? new List<ProjectWorkItem>();
            var results = new List<TimelineRowModel>();

            if (!itemList.Any())
                return results;

            // トップレベルアイテム（親がいない、またはリスト内に親が存在しないアイテム）から開始
            var rootItems = itemList
                .Where(t => !t.ParentId.HasValue || !itemList.Any(p => p.Id == t.ParentId.Value))
                .OrderBy(t => t.PlannedStartDate ?? DateTime.MaxValue)
                .ToList();

            foreach (var item in rootItems)
            {
                AppendItemRecursive(item, itemList, 0, results, referenceDate);
            }

            return results;
        }

        private void AppendItemRecursive(
            ProjectWorkItem item,
            List<ProjectWorkItem> allItems,
            int depth,
            List<TimelineRowModel> results,
            DateTime today
        )
        {
            var model = MapToRow(item, depth, today, allItems);
            results.Add(model);

            // 子アイテムを取得
            var children = allItems
                .Where(t => t.ParentId == item.Id)
                .OrderBy(t => t.PlannedStartDate ?? DateTime.MaxValue)
                .ToList();

            foreach (var child in children)
            {
                AppendItemRecursive(child, allItems, depth + 1, results, today);
            }
        }

        private TimelineRowModel MapToRow(
            ProjectWorkItem item,
            int depth,
            DateTime today,
            List<ProjectWorkItem> allItems
        )
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
                    // タイムライン描画は同期的である必要があるため、キャッシュから取得を試みる
                    // 未キャッシュの場合は IUserService の GetUserName 等の同期メソッドを活用する
                    var userName = _userService.GetUserName(task.Assignee.Value.ToString());
                    model.UserInitial = !string.IsNullOrEmpty(userName) ? userName[0].ToString().ToUpper() : "U";

                    // 色情報の同期取得手段がない場合はデフォルトを使用するが、
                    // 理想的には IUserService に GetUserByCache(Guid) 等があると良い。
                    // 現状はプレースホルダーとしておくか、Serviceを拡張する。
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
                    var children = allItems.Where(t => t.ParentId == item.Id).ToList();
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
