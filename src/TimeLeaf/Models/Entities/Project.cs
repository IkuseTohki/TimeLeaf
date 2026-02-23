using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TimeLeaf.Models.Enums;

namespace TimeLeaf.Models.Entities;

/// <summary>
/// プロジェクトを表すエンティティ。
/// </summary>
public class Project
{
    private readonly List<ProjectTask> _tasks = new();
    private readonly List<Milestone> _milestones = new();

    /// <summary>
    /// プロジェクトの作成日時。
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// プロジェクトの最終更新日時。
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// プロジェクトを一意に識別するID。
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// プロジェクト名。
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// プロジェクトの概要説明。
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// プロジェクトの状態。
    /// </summary>
    public ProjectStatus Status { get; private set; } = ProjectStatus.Initial;

    /// <summary>
    /// プロジェクトの健全性ステータス。
    /// </summary>
    public ProjectHealth HealthStatus { get; private set; } = ProjectHealth.Healthy;

    /// <summary>
    /// プロジェクトのマイルストーン（読み取り専用）。
    /// </summary>
    public IReadOnlyList<Milestone> Milestones => _milestones;

    /// <summary>
    /// プロジェクトに紐づくタスクのリスト（読み取り専用）。
    /// </summary>
    public IReadOnlyList<ProjectTask> Tasks => _tasks;

    /// <summary>
    /// デフォルトコンストラクタ。
    /// </summary>
    public Project() { }

    /// <summary>
    /// JSON デシリアライズ用コンストラクタ。
    /// </summary>
    [System.Text.Json.Serialization.JsonConstructor]
    public Project(Guid Id, string Name, string Description, ProjectStatus Status, ProjectHealth HealthStatus, DateTime CreatedAt, DateTime UpdatedAt, List<ProjectTask>? Tasks, List<Milestone>? Milestones)
    {
        this.Id = Id;
        this.Name = Name;
        this.Description = Description;
        this.Status = Status;
        this.HealthStatus = HealthStatus;
        this.CreatedAt = CreatedAt;
        this.UpdatedAt = UpdatedAt;
        if (Tasks != null) _tasks.AddRange(Tasks);
        if (Milestones != null) _milestones.AddRange(Milestones);
    }

    /// <summary>
    /// プロジェクト全体の合計見積工数。
    /// </summary>
    public double TotalEstimatedCost => _tasks.Sum(t => t.EstimatedCost);

    /// <summary>
    /// プロジェクト全体の合計実績工数。
    /// </summary>
    public double TotalActualCost => _tasks.Sum(t => t.ActualCost);

    /// <summary>
    /// プロジェクト名を更新します。
    /// </summary>
    public void UpdateName(string name)
    {
        if (Name == name) return;
        Name = name;
        RefreshUpdatedAt();
    }

    /// <summary>
    /// プロジェクトの概要を更新します。
    /// </summary>
    public void UpdateDescription(string description)
    {
        if (Description == description) return;
        Description = description;
        RefreshUpdatedAt();
    }

    /// <summary>
    /// プロジェクトのステータスを更新します。
    /// </summary>
    public void UpdateStatus(ProjectStatus status)
    {
        if (Status == status) return;
        Status = status;
        RefreshUpdatedAt();
    }

    /// <summary>
    /// プロジェクトの健全性を更新します。
    /// </summary>
    public void UpdateHealth(ProjectHealth health)
    {
        if (HealthStatus == health) return;
        HealthStatus = health;
        RefreshUpdatedAt();
    }

    /// <summary>
    /// プロジェクトにタスクを追加します。
    /// </summary>
    public void AddTask(ProjectTask task)
    {
        if (task == null) throw new ArgumentNullException(nameof(task));
        _tasks.Add(task);
        RefreshUpdatedAt();
    }

    /// <summary>
    /// プロジェクトからタスクを削除します。
    /// </summary>
    public void RemoveTask(Guid taskId)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == taskId);
        if (task != null)
        {
            _tasks.Remove(task);
            RefreshUpdatedAt();
        }
    }

    /// <summary>
    /// プロジェクトにマイルストーンを追加します。
    /// </summary>
    public void AddMilestone(Milestone milestone)
    {
        if (milestone == null) throw new ArgumentNullException(nameof(milestone));
        _milestones.Add(milestone);
        RefreshUpdatedAt();
    }

    /// <summary>
    /// プロジェクトのマイルストーンをすべてクリアします（再ロード用）。
    /// </summary>
    public void ClearMilestones()
    {
        _milestones.Clear();
    }

    /// <summary>
    /// プロジェクトのタスクをすべてクリアします（再ロード用）。
    /// </summary>
    public void ClearTasks()
    {
        _tasks.Clear();
    }

    /// <summary>
    /// 特定のカテゴリの変更があった場合に最終更新日時をリフレッシュします。
    /// </summary>
    public void RefreshUpdatedAt()
    {
        UpdatedAt = DateTime.Now;
    }
}
