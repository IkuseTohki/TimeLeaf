using System;
using System.Collections.Generic;
using System.Linq;
using TimeLeaf.Models.Enums;

namespace TimeLeaf.Models.Entities;

/// <summary>
/// タスクを束ねる「入れ物」を表すエンティティ。
/// マクロな計画や全体構造の構築に特化する。
/// </summary>
public class ProjectContainer : ProjectWorkItem
{
    private readonly List<ProjectWorkItem> _children = new();

    /// <summary>
    /// 子要素のリスト（読み取り専用）。
    /// </summary>
    public IReadOnlyList<ProjectWorkItem> Children => _children;

    /// <summary>
    /// デフォルトコンストラクタ。
    /// </summary>
    public ProjectContainer() { }

    /// <summary>
    /// 初期化用コンストラクタ。
    /// </summary>
    public ProjectContainer(Guid id, string name)
    {
        Id = id;
        Name = name;
    }

    /// <summary>
    /// 子要素を追加します。
    /// </summary>
    public void AddChild(ProjectWorkItem child)
    {
        if (child == null)
            throw new ArgumentNullException(nameof(child));
        if (child.Id == this.Id)
            throw new ArgumentException("Cannot add self as child.");

        child.SetParentId(this.Id);
        _children.Add(child);
    }

    /// <summary>
    /// 子要素を一括で読み込みます。
    /// </summary>
    public void LoadChildren(IEnumerable<ProjectWorkItem> children)
    {
        _children.Clear();
        if (children != null)
        {
            foreach (var child in children)
            {
                AddChild(child);
            }
        }
    }

    // 動的な導出プロパティ (DataModel.md に準拠)

    /// <summary>
    /// 進捗率（子要素の実績に基づく積み上げ）。
    /// </summary>
    public int ProgressPercentage
    {
        get
        {
            if (!_children.Any())
                return 0;
            return (int)_children.Average(c => GetWorkItemProgress(c));
        }
    }

    /// <summary>
    /// 実績工数（子要素の合計）。
    /// </summary>
    public double ActualHours => _children.Sum(c => GetWorkItemActualHours(c));

    /// <summary>
    /// 実績開始日（子の中で最も早い開始日）。
    /// </summary>
    public DateTime? ActualStartDate
    {
        get
        {
            var dates = _children.Select(c => GetWorkItemActualStartDate(c)).Where(d => d.HasValue).ToList();
            return dates.Any() ? dates.Min() : null;
        }
    }

    /// <summary>
    /// 実績終了日（すべての子が完了している場合、最も遅い終了日）。
    /// </summary>
    public DateTime? ActualEndDate
    {
        get
        {
            if (!_children.Any() || _children.Any(c => GetWorkItemStatus(c) != TaskStatus.Completed))
                return null;

            var dates = _children.Select(c => GetWorkItemActualEndDate(c)).Where(d => d.HasValue).ToList();
            return dates.Any() ? dates.Max() : null;
        }
    }

    /// <summary>
    /// 状態（子の状態で決定）。
    /// </summary>
    public TaskStatus Status
    {
        get
        {
            if (!_children.Any())
                return TaskStatus.NotStarted;
            if (_children.All(c => GetWorkItemStatus(c) == TaskStatus.Completed))
                return TaskStatus.Completed;
            if (_children.Any(c => GetWorkItemStatus(c) != TaskStatus.NotStarted))
                return TaskStatus.InProgress;
            return TaskStatus.NotStarted;
        }
    }

    // ヘルパーメソッド（Task/Container の差異を吸収）

    private int GetWorkItemProgress(ProjectWorkItem item) =>
        item switch
        {
            ProjectTask t => t.Status == TaskStatus.Completed ? 100 : (t.Status == TaskStatus.InProgress ? 50 : 0),
            ProjectContainer c => c.ProgressPercentage,
            _ => 0,
        };

    private double GetWorkItemActualHours(ProjectWorkItem item) =>
        item switch
        {
            ProjectTask t => t.ActualCost,
            ProjectContainer c => c.ActualHours,
            _ => 0,
        };

    private DateTime? GetWorkItemActualStartDate(ProjectWorkItem item) =>
        item switch
        {
            ProjectTask t => t.ActualStartDate,
            ProjectContainer c => c.ActualStartDate,
            _ => null,
        };

    private DateTime? GetWorkItemActualEndDate(ProjectWorkItem item) =>
        item switch
        {
            ProjectTask t => t.ActualEndDate,
            ProjectContainer c => c.ActualEndDate,
            _ => null,
        };

    private TaskStatus GetWorkItemStatus(ProjectWorkItem item) =>
        item switch
        {
            ProjectTask t => t.Status,
            ProjectContainer c => c.Status,
            _ => TaskStatus.NotStarted,
        };
}
