using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TimeLeaf.Models.Entities;

/// <summary>
/// プロジェクトを表すエンティティ。
/// </summary>
public class Project
{
    /// <summary>
    /// プロジェクト名。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// プロジェクトに紐づくタスクのリスト。
    /// </summary>
    public ObservableCollection<Task> Tasks { get; set; } = new();
}
