using System.Collections.Generic;

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
    public List<Task> Tasks { get; } = new();
}
