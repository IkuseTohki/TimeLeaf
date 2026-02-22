using System;
using TimeLeaf.Models.Enums;

namespace TimeLeaf.Models.Entities;

/// <summary>
/// タスクを表すエンティティ。
/// </summary>
public class ProjectTask
{
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
    /// 期限 (Due Date)。
    /// </summary>
    public DateTime? Deadline { get; set; }

    /// <summary>
    /// 見積工数。
    /// </summary>
    public double EstimatedCost { get; set; }

    /// <summary>
    /// 実績工数。
    /// </summary>
    public double ActualCost { get; set; }
}
