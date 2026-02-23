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
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// プロジェクトの概要説明。
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// プロジェクトの状態。
    /// </summary>
    public ProjectStatus Status { get; set; } = ProjectStatus.Initial;

    /// <summary>
    /// プロジェクトの健全性ステータス。
    /// </summary>
    public ProjectHealth HealthStatus { get; set; } = ProjectHealth.Healthy;

    /// <summary>
    /// プロジェクトのマイルストーン。
    /// </summary>
    public ObservableCollection<Milestone> Milestones { get; set; } = new();

    /// <summary>
    /// プロジェクトに紐づくタスクのリスト。
    /// </summary>
    public ObservableCollection<ProjectTask> Tasks { get; set; } = new();

    /// <summary>
    /// プロジェクト全体の合計見積工数。
    /// </summary>
    public double TotalEstimatedCost => Tasks.Sum(t => t.EstimatedCost);

    /// <summary>
    /// プロジェクト全体の合計実績工数。
    /// </summary>
    public double TotalActualCost => Tasks.Sum(t => t.ActualCost);
}
