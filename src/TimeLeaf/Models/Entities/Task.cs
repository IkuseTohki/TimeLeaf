using System;

namespace TimeLeaf.Models.Entities;

/// <summary>
/// タスクを表すエンティティ。
/// </summary>
public class Task
{
    /// <summary>
    /// タスクを一意に識別するID。
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// タスク名。
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
