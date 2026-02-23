using System;
using System.Collections.Generic;

namespace TimeLeaf.Models.Entities;

/// <summary>
/// タスクに対するコメントを表すエンティティ。
/// </summary>
public class Comment
{
    /// <summary>
    /// コメントの一意な識別子（GUID）。
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 紐づくタスクの参照ID。
    /// </summary>
    public Guid TaskId { get; set; }

    /// <summary>
    /// 投稿者のユーザーID。
    /// </summary>
    public string AuthorId { get; set; } = string.Empty;

    /// <summary>
    /// 投稿日時。
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// コメント本文（Markdown対応）。
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 添付ファイルへのポインタリスト。
    /// </summary>
    public List<string> AttachmentLinks { get; set; } = new();
}
