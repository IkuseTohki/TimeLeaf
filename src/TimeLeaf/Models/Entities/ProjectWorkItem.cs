using System;
using System.Collections.Generic;

namespace TimeLeaf.Models.Entities;

/// <summary>
/// タスクやコンテナの基底となる抽象クラス。
/// 共通の属性（ID、名前、計画日程、制約等）を保持する。
/// </summary>
public abstract class ProjectWorkItem
{
    protected readonly List<TaskConstraint> _constraints = new();
    protected readonly List<Guid> _watcherIds = new();
    protected readonly List<Guid> _relatedTaskIds = new();

    /// <summary>
    /// アイテムを一意に識別するID。
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// 所属するプロジェクトのID。
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// 親アイテムのID。
    /// </summary>
    public Guid? ParentId { get; protected set; }

    /// <summary>
    /// アイテム名。
    /// </summary>
    public string Name { get; protected set; } = string.Empty;

    /// <summary>
    /// 詳細説明。
    /// </summary>
    public string Description { get; protected set; } = string.Empty;

    /// <summary>
    /// 開始予定日。
    /// </summary>
    public DateTime? PlannedStartDate { get; protected set; }

    /// <summary>
    /// 終了予定日。
    /// </summary>
    public DateTime? PlannedEndDate { get; protected set; }

    /// <summary>
    /// 期限 (Deadline)。
    /// </summary>
    public DateTime? Deadline { get; protected set; }

    /// <summary>
    /// 必要日数。
    /// </summary>
    public double RequiredDays { get; protected set; }

    /// <summary>
    /// 制約のリスト（読み取り専用）。
    /// </summary>
    public IReadOnlyList<TaskConstraint> Constraints => _constraints;

    /// <summary>
    /// ウォッチャーのIDリスト（読み取り専用）。
    /// </summary>
    public IReadOnlyList<Guid> WatcherIds => _watcherIds;

    /// <summary>
    /// 関連タスクのIDリスト（読み取り専用）。
    /// </summary>
    public IReadOnlyList<Guid> RelatedTaskIds => _relatedTaskIds;

    /// <summary>
    /// 親IDを設定します。
    /// </summary>
    public virtual void SetParentId(Guid? parentId)
    {
        ParentId = parentId;
    }

    /// <summary>
    /// 名前を更新します。
    /// </summary>
    public virtual void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));
        Name = name;
    }

    /// <summary>
    /// 詳細説明を更新します。
    /// </summary>
    public virtual void UpdateDescription(string description)
    {
        Description = description ?? string.Empty;
    }

    /// <summary>
    /// スケジュール（予定開始日・終了日・期限）を更新します。
    /// </summary>
    public virtual void UpdateSchedule(DateTime? start, DateTime? end, DateTime? deadline)
    {
        PlannedStartDate = start;
        PlannedEndDate = end;
        Deadline = deadline;
    }

    /// <summary>
    /// 必要日数を更新します。
    /// </summary>
    public virtual void UpdateRequiredDays(double days)
    {
        if (days < 0)
            throw new ArgumentException("Required days cannot be negative.", nameof(days));
        RequiredDays = days;
    }

    /// <summary>
    /// 制約を追加します。
    /// </summary>
    public virtual void AddConstraint(TaskConstraint constraint)
    {
        if (constraint == null)
            throw new ArgumentNullException(nameof(constraint));
        _constraints.Add(constraint);
    }

    /// <summary>
    /// 制約を一括で読み込みます。
    /// </summary>
    public virtual void LoadConstraints(IEnumerable<TaskConstraint> constraints)
    {
        _constraints.Clear();
        if (constraints != null)
            _constraints.AddRange(constraints);
    }
}
