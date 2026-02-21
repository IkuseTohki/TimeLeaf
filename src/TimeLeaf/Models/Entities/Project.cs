using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TimeLeaf.Models.Entities;

/// <summary>
/// プロジェクトを表すエンティティ。
/// </summary>
public class Project
{
    /// <summary>
    /// プロジェクトを一意に識別するID。
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// プロジェクト名。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// プロジェクトに紐づくタスクのリスト。
    /// </summary>
    public ObservableCollection<Task> Tasks { get; set; } = new();
}
