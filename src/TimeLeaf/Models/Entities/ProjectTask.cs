using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using TimeLeaf.Models.Enums;

namespace TimeLeaf.Models.Entities;

/// <summary>
/// タスクを表すエンティティ。
/// </summary>
public class ProjectTask : ProjectWorkItem
{
    private readonly List<Comment> _comments = new();
    private readonly List<ProjectTask> _children = new();
    private readonly List<Guid> _dependencies = new();

    /// <summary>
    /// タスクの進捗状態。
    /// </summary>
    public TaskStatus Status { get; private set; } = TaskStatus.NotStarted;

    /// <summary>
    /// タスクの優先度。
    /// </summary>
    public TaskPriority Priority { get; private set; } = TaskPriority.Medium;

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
    /// 予定開始日（ProjectWorkItem.PlannedStartDate へのエイリアス）。
    /// </summary>
    [JsonIgnore]
    public DateTime? ScheduledStartDate => PlannedStartDate;

    /// <summary>
    /// 子タスクのリスト（読み取り専用）。
    /// </summary>
    [JsonIgnore]
    public IReadOnlyList<ProjectTask> Children => _children;

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
        Guid id,
        string name,
        string description,
        TaskStatus status,
        TaskPriority priority,
        DateTime? scheduledStartDate,
        DateTime? deadline,
        DateTime? actualStartDate,
        DateTime? actualEndDate,
        double estimatedCost,
        double actualCost,
        string assignee,
        List<TaskConstraint>? constraints,
        List<Comment>? comments
    )
    {
        if (id != Guid.Empty)
            this.Id = id;
        this.Name = name;
        this.Description = description;
        this.Status = status;
        this.Priority = priority;
        this.PlannedStartDate = scheduledStartDate;
        this.Deadline = deadline;
        this.ActualStartDate = actualStartDate;
        this.ActualEndDate = actualEndDate;
        this.EstimatedCost = estimatedCost;
        this.ActualCost = actualCost;
        this.Assignee = assignee;
        if (constraints != null)
        {
            _constraints.AddRange(constraints);
            _dependencies.AddRange(constraints.Select(c => c.PredecessorId));
        }
        if (comments != null)
            _comments.AddRange(comments);
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
        PlannedStartDate = scheduledStart;
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
    public override void AddConstraint(TaskConstraint constraint)
    {
        base.AddConstraint(constraint);
        if (!_dependencies.Contains(constraint.PredecessorId))
            _dependencies.Add(constraint.PredecessorId);
    }

    /// <summary>
    /// 既存の制約を一括で追加します（再ロード時等に使用）。
    /// </summary>
    public override void LoadConstraints(IEnumerable<TaskConstraint> constraints)
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

        child.SetParentId(this.Id);
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
            child.SetParentId(this.Id);
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
        PlannedStartDate = starts.Any() ? starts.Min() : null;

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

    /// <summary>
    /// このタスクのディープコピーを作成します。
    /// </summary>
    /// <returns>複製された ProjectTask インスタンス。</returns>
    public ProjectTask Clone()
    {
        var clone = new ProjectTask(
            this.Id,
            this.Name,
            this.Description,
            this.Status,
            this.Priority,
            this.ScheduledStartDate,
            this.Deadline,
            this.ActualStartDate,
            this.ActualEndDate,
            this.EstimatedCost,
            this.ActualCost,
            this.Assignee,
            this.Constraints.ToList(),
            this.Comments.Select(c => new Comment(
                    c.Id,
                    c.TaskId,
                    c.AuthorId,
                    c.CreatedAt,
                    c.Content,
                    c.AttachmentLinks.ToList()
                ))
                .ToList()
        );
        clone.SetParentId(this.ParentId);
        return clone;
    }

    /// <summary>
    /// 指定されたタスクの内容をこのタスクにマージ（反映）します。
    /// ID は変更されません。
    /// </summary>
    /// <param name="source">コピー元となるタスク。</param>
    public void MergeFrom(ProjectTask source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        UpdateName(source.Name);
        UpdateDescription(source.Description);
        UpdateStatus(source.Status);
        UpdatePriority(source.Priority);
        UpdateSchedule(source.ScheduledStartDate, source.Deadline);
        UpdateActualDates(source.ActualStartDate, source.ActualEndDate);
        UpdateEstimatedCost(source.EstimatedCost);
        UpdateActualCost(source.ActualCost);
        AssignTo(source.Assignee);

        LoadConstraints(source.Constraints);
        LoadComments(source.Comments);
    }
}
