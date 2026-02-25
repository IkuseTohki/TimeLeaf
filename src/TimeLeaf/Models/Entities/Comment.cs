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
    public Guid Id { get; init; } = Guid.NewGuid();

    private readonly Guid _taskId;
    /// <summary>
    /// 紐づくタスクの参照ID。
    /// </summary>
    public Guid TaskId
    {
        get => _taskId;
        init
        {
            if (value == Guid.Empty)
                throw new ArgumentException("TaskId cannot be empty.", nameof(value));
            _taskId = value;
        }
    }

    private readonly string _authorId = string.Empty;
    /// <summary>
    /// 投稿者のユーザーID。
    /// </summary>
    public string AuthorId
    {
        get => _authorId;
        init
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("AuthorId cannot be empty.", nameof(value));
            _authorId = value;
        }
    }

    /// <summary>
    /// 投稿日時。
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    private readonly string _content = string.Empty;
    /// <summary>
    /// コメント本文（Markdown対応）。
    /// </summary>
    public string Content
    {
        get => _content;
        init
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Comment content cannot be empty.", nameof(value));
            _content = value;
        }
    }

    /// <summary>
    /// 添付ファイルへのポインタリスト。
    /// </summary>
    public List<string> AttachmentLinks { get; init; } = new();

    /// <summary>
    /// デフォルトコンストラクタ（シリアライズ用）。
    /// </summary>
    public Comment() { }

    /// <summary>
    /// パラメータ付きコンストラクタ。
    /// </summary>
    [System.Text.Json.Serialization.JsonConstructor]
    public Comment(Guid id, Guid taskId, string authorId, DateTime createdAt, string content, List<string>? attachmentLinks)
    {
        Id = id;
        TaskId = taskId;
        AuthorId = authorId;
        CreatedAt = createdAt;
        Content = content;
        AttachmentLinks = attachmentLinks ?? new();
    }
}
