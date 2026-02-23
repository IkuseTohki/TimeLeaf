using System;
using System.Collections.Generic;
using TimeLeaf.Models.Enums;

namespace TimeLeaf.Models.Entities;

/// <summary>
/// タスクを表すエンティティ。
/// </summary>
public class ProjectTask
{
    private readonly List<Comment> _comments = new();

    /// <summary>
    /// タスクを一意に識別するID。
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// タスク名。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// タスクの詳細説明。
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// タスクの進捗状態。
    /// </summary>
    public TaskStatus Status { get; set; } = TaskStatus.NotStarted;

    /// <summary>
    /// タスクの優先度。
    /// </summary>
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    /// <summary>
    /// 開始予定日。
    /// </summary>
    public DateTime? ScheduledStartDate { get; set; }

    /// <summary>
    /// 期限 (Due Date)。
    /// </summary>
    public DateTime? Deadline { get; set; }

    /// <summary>
    /// 実際の作業開始日。
    /// </summary>
    public DateTime? ActualStartDate { get; set; }

    /// <summary>
    /// 実際の作業完了日。
    /// </summary>
    public DateTime? ActualEndDate { get; set; }

    /// <summary>
    /// 見積工数。
    /// </summary>
    public double EstimatedCost { get; set; }

    /// <summary>
    /// 実績工数。
    /// </summary>
    public double ActualCost { get; set; }

    /// <summary>
    /// 作業担当者。
    /// </summary>
    public string Assignee { get; set; } = string.Empty;

    /// <summary>
    /// 依存タスクのIDリスト。
    /// </summary>
    public List<Guid> Dependencies { get; set; } = new();

    /// <summary>
    /// タスクに関するコメントのリスト（読み取り専用）。
    /// </summary>
    public IReadOnlyList<Comment> Comments => _comments;

    /// <summary>
    /// デフォルトコンストラクタ。
    /// </summary>
    public ProjectTask() { }

    /// <summary>
    /// JSON デシリアライズ用コンストラクタ。
    /// </summary>
    [System.Text.Json.Serialization.JsonConstructor]
    public ProjectTask(Guid Id, string Name, string Description, TaskStatus Status, TaskPriority Priority,
        DateTime? ScheduledStartDate, DateTime? Deadline, DateTime? ActualStartDate, DateTime? ActualEndDate,
        double EstimatedCost, double ActualCost, string Assignee, List<Guid>? Dependencies, List<Comment>? Comments)
    {
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
        this.Dependencies = Dependencies ?? new();
        if (Comments != null) _comments.AddRange(Comments);
    }

    /// <summary>
    /// コメントを追加します。
    /// </summary>
    public void AddComment(Comment comment)
    {
        if (comment == null) throw new ArgumentNullException(nameof(comment));
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
}
