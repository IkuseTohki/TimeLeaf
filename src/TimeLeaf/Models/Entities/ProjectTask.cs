using System;
using System.Collections.Generic;
using System.Linq;
using TimeLeaf.Models.Enums;

namespace TimeLeaf.Models.Entities;

/// <summary>
/// タスクを表すエンティティ。
/// </summary>
public class ProjectTask
{
    private readonly List<Comment> _comments = new();
    private readonly List<TaskConstraint> _constraints = new();
    private readonly List<ProjectTask> _children = new();
    private readonly List<Guid> _dependencies = new();

    /// <summary>
    /// タスクを一意に識別するID。
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// 親タスクのID。
    /// </summary>
    public Guid? ParentId { get; private set; }

    /// <summary>
    /// 親タスクIDを設定します（リポジトリ復元用）。
    /// </summary>
    internal void SetParentId(Guid? parentId)
    {
        ParentId = parentId;
    }

    /// <summary>
    /// 子タスクのリスト（読み取り専用）。
    /// </summary>
    public IReadOnlyList<ProjectTask> Children => _children;

    /// <summary>
    /// タスク名。
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// タスクの詳細説明。
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// タスクの進捗状態。
    /// </summary>
    public TaskStatus Status { get; private set; } = TaskStatus.NotStarted;

    /// <summary>
    /// タスクの優先度。
    /// </summary>
    public TaskPriority Priority { get; private set; } = TaskPriority.Medium;

    /// <summary>
    /// 開始予定日。
    /// </summary>
    public DateTime? ScheduledStartDate { get; private set; }

    /// <summary>
    /// 期限 (Due Date)。
    /// </summary>
    public DateTime? Deadline { get; private set; }

    /// <summary>
    /// 実際の作業開始日。
    /// </summary>
    public DateTime? ActualStartDate { get; private set; }

    /// <summary>
    /// 実際の作業完了日。
    /// </summary>
    public DateTime? ActualEndDate { get; private set; }

    /// <summary>
    /// 見積工数。
    /// </summary>
    public double EstimatedCost { get; private set; }

    /// <summary>
    /// 実績工数。
    /// </summary>
    public double ActualCost { get; private set; }

    /// <summary>
    /// 作業担当者。
    /// </summary>
    public string Assignee { get; private set; } = string.Empty;

    /// <summary>
    /// タスク間の制約（依存関係）のリスト（読み取り専用）。
    /// </summary>
    public IReadOnlyList<TaskConstraint> Constraints => _constraints;

    /// <summary>
    /// 依存タスクのIDリスト。
    /// </summary>
    [Obsolete("Use Constraints instead.")]
    public List<Guid> Dependencies => _dependencies;

    /// <summary>
    /// タスクに関するコメントのリスト（読み取り専用）。
    /// </summary>
    public IReadOnlyList<Comment> Comments => _comments;

    /// <summary>
    /// デフォルトコンストラクタ（シリアライズ用）。
    /// </summary>
    public ProjectTask() { }

    /// <summary>
    /// JSON デシリアライズ用コンストラクタ。
    /// </summary>
    [System.Text.Json.Serialization.JsonConstructor]
    public ProjectTask(
        Guid Id,
        string Name,
        string Description,
        TaskStatus Status,
        TaskPriority Priority,
        DateTime? ScheduledStartDate,
        DateTime? Deadline,
        DateTime? ActualStartDate,
        DateTime? ActualEndDate,
        double EstimatedCost,
        double ActualCost,
        string Assignee,
        List<TaskConstraint>? Constraints,
        List<Comment>? Comments
    )
    {
        if (Id != Guid.Empty)
            this.Id = Id;
        this.Name = Name;
        this.Description = Description;
        this.Status = Status;
        this.Priority = Priority;
        this.ScheduledStartDate = ScheduledStartDate;
        this.Deadline = Deadline;
        this.ActualStartDate = ActualStartDate;
        this.ActualEndDate = ActualEndDate;
        this.EstimatedCost = EstimatedCost;
        this.ActualCost = ActualCost;
        this.Assignee = Assignee;
        if (Constraints != null)
        {
            _constraints.AddRange(Constraints);
            _dependencies.AddRange(Constraints.Select(c => c.PredecessorId));
        }
        if (Comments != null)
            _comments.AddRange(Comments);
    }

    /// <summary>
    /// タスク名を更新します。
    /// </summary>
    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Task name cannot be empty.", nameof(name));
        Name = name;
    }

    /// <summary>
    /// タスクの詳細を更新します。
    /// </summary>
    public void UpdateDescription(string description)
    {
        Description = description ?? string.Empty;
    }

    /// <summary>
    /// タスクのステータスを更新します。
    /// 状態遷移に伴い、開始日・終了日の自動設定も行います。
    /// </summary>
    public void UpdateStatus(TaskStatus status)
    {
        if (Status == status)
            return;

        Status = status;

        if (status == TaskStatus.InProgress && ActualStartDate == null)
        {
            ActualStartDate = DateTime.Now;
        }
        else if (status == TaskStatus.Completed && ActualEndDate == null)
        {
            ActualEndDate = DateTime.Now;
            if (ActualStartDate == null)
                ActualStartDate = DateTime.Now; // 未開始のまま完了した場合は開始日も埋める
        }
    }

    /// <summary>
    /// 優先度を更新します。
    /// </summary>
    public void UpdatePriority(TaskPriority priority)
    {
        Priority = priority;
    }

    /// <summary>
    /// スケジュール（予定開始日・期限）を一括更新します。
    /// </summary>
    public void UpdateSchedule(DateTime? scheduledStart, DateTime? deadline)
    {
        ScheduledStartDate = scheduledStart;
        Deadline = deadline;
    }

    /// <summary>
    /// 実績（開始日・終了日）を手動で更新します。
    /// </summary>
    public void UpdateActualDates(DateTime? start, DateTime? end)
    {
        ActualStartDate = start;
        ActualEndDate = end;
    }

    /// <summary>
    /// 見積工数を更新します。
    /// </summary>
    public void UpdateEstimatedCost(double cost)
    {
        if (cost < 0)
            throw new ArgumentException("Cost cannot be negative.", nameof(cost));
        EstimatedCost = cost;
    }

    /// <summary>
    /// 実績工数を更新します。
    /// </summary>
    public void UpdateActualCost(double cost)
    {
        if (cost < 0)
            throw new ArgumentException("Cost cannot be negative.", nameof(cost));
        ActualCost = cost;
    }

    /// <summary>
    /// 担当者を更新します。
    /// </summary>
    public void AssignTo(string assignee)
    {
        Assignee = assignee ?? string.Empty;
    }

    /// <summary>
    /// コメントを追加します。
    /// </summary>
    public void AddComment(Comment comment)
    {
        if (comment == null)
            throw new ArgumentNullException(nameof(comment));
        _comments.Add(comment);
    }

    /// <summary>
    /// 既存のコメントを一括で追加します（再ロード時等に使用）。
    /// </summary>
    public void LoadComments(IEnumerable<Comment> comments)
    {
        _comments.Clear();
        _comments.AddRange(comments);
    }

    /// <summary>
    /// 制約を追加します。
    /// </summary>
    public void AddConstraint(TaskConstraint constraint)
    {
        if (constraint == null)
            throw new ArgumentNullException(nameof(constraint));
        _constraints.Add(constraint);

        if (!_dependencies.Contains(constraint.PredecessorId))
            _dependencies.Add(constraint.PredecessorId);
    }

    /// <summary>
    /// 既存の制約を一括で追加します（再ロード時等に使用）。
    /// </summary>
    public void LoadConstraints(IEnumerable<TaskConstraint> constraints)
    {
        _constraints.Clear();
        _dependencies.Clear();
        if (constraints != null)
        {
            foreach (var c in constraints)
            {
                AddConstraint(c);
            }
        }
    }

    /// <summary>
    /// 子タスクを追加します。
    /// </summary>
    public void AddChild(ProjectTask child)
    {
        if (child == null)
            throw new ArgumentNullException(nameof(child));
        if (child.Id != Guid.Empty && child.Id == this.Id)
            throw new ArgumentException("Cannot add self as child.", nameof(child));

        child.ParentId = this.Id;
        _children.Add(child);
        AggregateChildren();
    }

    /// <summary>
    /// 既存の子タスクを一括で追加します（再ロード時等に使用）。
    /// </summary>
    public void LoadChildren(IEnumerable<ProjectTask> children)
    {
        _children.Clear();
        foreach (var child in children)
        {
            child.ParentId = this.Id;
            _children.Add(child);
        }
        AggregateChildren();
    }

    /// <summary>
    /// 子タスクの情報を元に、自身のスケジュールと進捗を自動集計します。
    /// </summary>
    private void AggregateChildren()
    {
        if (!_children.Any())
            return;

        // スケジュールの集計
        var starts = _children
            .Where(c => c.ScheduledStartDate.HasValue)
            .Select(c => c.ScheduledStartDate!.Value)
            .ToList();
        ScheduledStartDate = starts.Any() ? starts.Min() : null;

        var ends = _children.Where(c => c.Deadline.HasValue).Select(c => c.Deadline!.Value).ToList();
        Deadline = ends.Any() ? ends.Max() : null;

        // 進捗の集計
        double averageProgress = _children.Average(c => GetProgressValue(c.Status));
        if (averageProgress >= 1.0)
            Status = TaskStatus.Completed;
        else if (averageProgress <= 0.0)
            Status = TaskStatus.NotStarted;
        else
            Status = TaskStatus.InProgress;
    }

    private double GetProgressValue(TaskStatus status) =>
        status switch
        {
            TaskStatus.Completed => 1.0,
            TaskStatus.InProgress => 0.5,
            TaskStatus.InReview => 0.8,
            _ => 0.0,
        };
}
