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
    private readonly List<Guid> _assignedUserIds = new();

    /// <summary>
    /// プロジェクトの作成日時。
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.Now;

    /// <summary>
    /// プロジェクトの最終更新日時。
    /// </summary>
    public DateTime UpdatedAt { get; private set; } = DateTime.Now;

    /// <summary>
    /// プロジェクトを一意に識別するID。
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

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
    /// プロジェクトがアーカイブされているかどうか。
    /// </summary>
    public bool IsArchived { get; private set; } = false;

    /// <summary>
    /// プロジェクトがロックされている期限（nullの場合はロックなし）。
    /// </summary>
    public DateTime? LockedUntil { get; private set; }

    /// <summary>
    /// プロジェクトのマイルストーン（読み取り専用）。
    /// </summary>
    public IReadOnlyList<Milestone> Milestones => _milestones;

    /// <summary>
    /// プロジェクトに紐づくタスクのリスト（読み取り専用）。
    /// </summary>
    public IReadOnlyList<ProjectTask> Tasks => _tasks;

    /// <summary>
    /// プロジェクトにアサインされているユーザーのID（読み取り専用）。
    /// </summary>
    public IReadOnlyList<Guid> AssignedUserIds => _assignedUserIds;

    /// <summary>
    /// デフォルトコンストラクタ。
    /// </summary>
    public Project() { }

    /// <summary>
    /// JSON デシリアライズ用コンストラクタ。
    /// </summary>
    [System.Text.Json.Serialization.JsonConstructor]
    public Project(
        Guid Id,
        string Name,
        string Description,
        ProjectStatus Status,
        ProjectHealth HealthStatus,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        List<ProjectTask>? Tasks,
        List<Milestone>? Milestones
    )
    {
        this.Id = Id;
        this.Name = Name;
        this.Description = Description;
        this.Status = Status;
        this.HealthStatus = HealthStatus;
        this.CreatedAt = CreatedAt;
        this.UpdatedAt = UpdatedAt;
        if (Tasks != null)
            _tasks.AddRange(Tasks);
        if (Milestones != null)
            _milestones.AddRange(Milestones);
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
        if (Name == name)
            return;
        Name = name;
    }

    /// <summary>
    /// プロジェクトのステータスを更新します。
    /// </summary>
    public void UpdateStatus(ProjectStatus status)
    {
        if (Status == status)
            return;
        Status = status;
    }

    /// <summary>
    /// プロジェクトの健全性を更新します。
    /// </summary>
    public void UpdateHealth(ProjectHealth health)
    {
        if (HealthStatus == health)
            return;
        HealthStatus = health;
    }

    /// <summary>
    /// プロジェクトの基本情報を更新します。
    /// </summary>
    public void UpdateBasicInfo(string name, ProjectStatus status, ProjectHealth health)
    {
        UpdateName(name);
        UpdateStatus(status);
        UpdateHealth(health);
    }

    /// <summary>
    /// マイルストーンを一括で再設定します（履歴再生用）。
    /// </summary>
    public void ReplayMilestones(IEnumerable<Milestone> milestones)
    {
        _milestones.Clear();
        if (milestones != null)
        {
            _milestones.AddRange(milestones);
        }
    }

    /// <summary>
    /// プロジェクトの概要を更新します。
    /// </summary>
    public void UpdateDescription(string description)
    {
        if (Description == description)
            return;
        Description = description;
    }

    /// <summary>
    /// プロジェクトにタスクを追加します。
    /// </summary>
    public void AddTask(ProjectTask task)
    {
        if (task == null)
            throw new ArgumentNullException(nameof(task));
        _tasks.Add(task);
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
        }
    }

    /// <summary>
    /// プロジェクトのタスクをすべてクリアします（再ロード用）。
    /// </summary>
    public void ClearTasks()
    {
        _tasks.Clear();
    }

    /// <summary>
    /// プロジェクトにマイルストーンを追加します。
    /// </summary>
    public void AddMilestone(Milestone milestone)
    {
        if (milestone == null)
            throw new ArgumentNullException(nameof(milestone));
        _milestones.Add(milestone);
    }

    /// <summary>
    /// プロジェクトにユーザーをアサインします。
    /// </summary>
    public void AssignUser(Guid userId)
    {
        if (!_assignedUserIds.Contains(userId))
        {
            _assignedUserIds.Add(userId);
        }
    }

    /// <summary>
    /// プロジェクトのユーザーアサインを解除します。
    /// </summary>
    public void UnassignUser(Guid userId)
    {
        _assignedUserIds.Remove(userId);
    }

    /// <summary>
    /// アサイン情報を一括で再設定します（履歴再生用）。
    /// </summary>
    public void ReplayAssignments(IEnumerable<Guid> userIds)
    {
        _assignedUserIds.Clear();
        if (userIds != null)
        {
            foreach (var id in userIds)
            {
                AssignUser(id);
            }
        }
    }

    /// <summary>
    /// 特定のカテゴリの変更があった場合に最終更新日時をリフレッシュします。
    /// </summary>
    public void RefreshUpdatedAt()
    {
        UpdatedAt = DateTime.Now;
    }

    /// <summary>
    /// 最終更新日時を明示的に設定します（同期用）。
    /// </summary>
    public void SetUpdatedAt(DateTime updatedAt)
    {
        UpdatedAt = updatedAt;
    }

    /// <summary>
    /// プロジェクトをアーカイブ状態にします。
    /// </summary>
    public void Archive()
    {
        IsArchived = true;
    }

    /// <summary>
    /// プロジェクトのアーカイブ状態を解除します。
    /// </summary>
    public void Unarchive()
    {
        IsArchived = false;
    }

    /// <summary>
    /// プロジェクトを指定日時までロックします。
    /// </summary>
    public void Lock(DateTime until)
    {
        LockedUntil = until;
    }

    /// <summary>
    /// プロジェクトのロックを解除します。
    /// </summary>
    public void Unlock()
    {
        LockedUntil = null;
    }

    /// <summary>
    /// ライフサイクル状態を設定します（リポジトリ復元用）。
    /// </summary>
    public void SetLifecycleStatus(bool isArchived, DateTime? lockedUntil)
    {
        IsArchived = isArchived;
        LockedUntil = lockedUntil;
    }
}
